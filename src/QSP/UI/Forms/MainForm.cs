using QSP.AircraftProfiles;
using QSP.Common.Options;
using QSP.GoogleMap;
using QSP.LibraryExtension;
using QSP.MathTools;
using QSP.Metar;
using QSP.RouteFinding;
using QSP.RouteFinding.Airports;
using QSP.RouteFinding.AirwayStructure;
using QSP.RouteFinding.Containers;
using QSP.RouteFinding.FileExport;
using QSP.RouteFinding.FileExport.Providers;
using QSP.RouteFinding.RouteAnalyzers;
using QSP.RouteFinding.Routes;
using QSP.RouteFinding.Routes.TrackInUse;
using QSP.RouteFinding.TerminalProcedures.Sid;
using QSP.RouteFinding.Tracks.Common;
using QSP.UI;
using QSP.UI.Controllers;
using QSP.Utilities;
using QSP.Utilities.Units;
using QSP.WindAloft;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static QSP.AviationTools.Constants;
using static QSP.RouteFinding.RouteFindingCore;
using static QSP.Utilities.LoggerInstance;

namespace QSP
{
    public partial class MainForm
    {
        public int OperatingEmptyWtKg;
        //OperatingEmptyWt = Basic Operating Wt
        public int MissedAppFuelKG;
        public int MaxZfwKg;

        private FormStateSaver formStateManagerFuel;
        private FormStateSaver formStateManagerTO;

        private ViewManager viewChanger;

        private AppOptions appSettings;
        private AirportManager airportList;
        private WaypointList wptList;

        private RouteFinderSelection origAirport;
        private RouteFinderSelection destAirport;
        private RouteFinderSelection altnAirport;

        #region "FuelCalculation"

        public static FuelCalculator ComputeFuelIteration(FuelCalculationParameters para, uint precisionLevel)
        {
            //presisionLevel = 0, 1, 2, ... 
            //smaller num = less precise
            //0 = disregard wind completely, 1 is good enough

            var FuelCalc = new FuelCalculator(para);
            var OptCrzCalc = new OptCrzCalculator(para.AC);

            //calculate altn first
            double fuelTon = 0;
            double avgWeightTon = 0;
            double crzAltFt = 0;
            int tailwind = 0;
            double tas = 0;

            for (uint i = 0; i <= precisionLevel; i++)
            {
                fuelTon = FuelCalc.GetAltnFuelTon();
                avgWeightTon = FuelCalc.LandWeightTonAltn + fuelTon / 2;
                crzAltFt = OptCrzCalc.ActualCrzAlt(avgWeightTon, para.DisToAltn);
                tas = OptCrzCalc.CruiseTas(crzAltFt);
                tailwind = computeTailWind(TailWindCalcOptions.DestToAltn, Convert.ToInt32(tas), Convert.ToInt32(crzAltFt / 100));
                para.AvgWindToAltn = tailwind;

                Debug.WriteLine("TO ALTN, CRZ ALT {0} FT, TAS {1} KTS, TAILWIND {2} KTS", crzAltFt, tas, tailwind);
            }

            for (uint i = 0; i <= precisionLevel; i++)
            {
                fuelTon = FuelCalc.GetDestFuelTon();
                avgWeightTon = FuelCalc.LandWeightTonDest + fuelTon / 2;
                crzAltFt = OptCrzCalc.ActualCrzAlt(avgWeightTon, para.DisToDest);
                tas = OptCrzCalc.CruiseTas(crzAltFt);
                tailwind = computeTailWind(TailWindCalcOptions.OrigToDest, Convert.ToInt32(tas), Convert.ToInt32(crzAltFt / 100));
                para.AvgWindToDest = tailwind;

                Debug.WriteLine("TO DEST, CRZ ALT {0} FT, TAS {1} KTS, TAILWIND {2} KTS", crzAltFt, tas, tailwind);

            }
            return FuelCalc;
        }

        private void Calculate(object sender, EventArgs e)
        {
            FuelReport_TxtBox.ForeColor = Color.Black;
            FuelReport_TxtBox.Text = "";

            FuelCalculationParameters Parameters = new FuelCalculationParameters();
            Parameters.FillInDefaultValueIfLeftBlank();

            try
            {
                Parameters.ImportValues();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return;
            }

            FuelCalculator FuelCalc = null;
            try
            {
                FuelCalc = ComputeFuelIteration(Parameters, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }

            double FuelToAltnTon = FuelCalc.GetAltnFuelTon();
            double FuelToDestTon = FuelCalc.GetDestFuelTon();

            FuelReportResult fuelCalcResult = new FuelReportResult(FuelToDestTon, FuelToAltnTon, Parameters, FuelCalc);


            if (fuelCalcResult.TotalFuelKG > FuelCalc.maxFuelKg)
            {
                MessageBox.Show(insufficientFuelMsg(fuelCalcResult.TotalFuelKG, FuelCalc.maxFuelKg, Parameters.WtUnit));
                return;

            }

            string OutputText = fuelCalcResult.ToString(Parameters.WtUnit);

            FuelReport_TxtBox.Text = Environment.NewLine + Strings.ShiftToRight(OutputText, 20);
            formStateManagerFuel.Save();

            //send weights to takeoff/ldg calc form 
            AC_Req = ACList.Text;
            TOWT_Req_Unit = Parameters.WtUnit;
            //TODO:        LDG_fuel_prediction_unit = Parameters.WtUnit();

            TOWT_Req = Convert.ToInt32(Parameters.Zfw + fuelCalcResult.TakeoffFuelKg * (Parameters.WtUnit == WeightUnit.KG ? 1.0 : KgLbRatio));
            //TODO:       LDG_ZFW = Convert.ToInt32(Parameters.Zfw);
            //TODO:  LDG_fuel_prediction = Convert.ToInt32(fuelCalcResult.LdgFuelKgPredict * (Parameters.WtUnit() == WeightUnit.KG ? 1.0 : KG_LB));

            viewChanger.ShowPage(ViewManager.Pages.FuelReport);
        }

        public enum TailWindCalcOptions
        {
            OrigToDest,
            DestToAltn
        }

        private static string insufficientFuelMsg(double fuelReqKG, double fuelCapacityKG, WeightUnit unit)
        {
            if (unit == WeightUnit.KG)
            {
                return "Insufficient fuel" + Environment.NewLine + "Fuel required for this flight is " + fuelReqKG + " KG. Maximum fuel tank capacity is " + fuelCapacityKG + " KG.";
            }
            else
            {
                return "Insufficient fuel" + Environment.NewLine + "Fuel required for this flight is " + Math.Round(fuelReqKG * KgLbRatio) + " LB. Maximum fuel tank capacity is " + Math.Round(fuelCapacityKG * KgLbRatio) + " LB.";
            }
        }

        #endregion

        public void Init(
            ProfileManager profiles,
            AppOptions appSettings,
            AirportManager airportList,
            WaypointList wptList)
        {
            initAircraftData(profiles);

            this.appSettings = appSettings;
            this.airportList = airportList;
            this.wptList = wptList;

            initRouteFinderSelections();
            advancedRouteTool.Init(
                appSettings, wptList, airportList, new TrackInUseCollection()); //TODO: track in use is wrong
        }

        private void initRouteFinderSelections()
        {
            origAirport = new RouteFinderSelection(
                OrigTxtBox,
                true,
                OrigRwyComboBox,
                OrigSidComboBox,
                appSettings,
                airportList,
                wptList);

            origAirport.Subscribe();

            destAirport = new RouteFinderSelection(
                DestTxtBox,
                false,
                DestRwyComboBox,
                DestStarComboBox,
                appSettings,
                airportList,
                wptList);

            destAirport.Subscribe();

            altnAirport = new RouteFinderSelection(
                AltnTxtBox,
                false,
                AltnRwyComboBox,
                AltnStarComboBox,
                appSettings,
                airportList,
                wptList);

            altnAirport.Subscribe();
        }

        private void initAircraftData(ProfileManager profiles)
        {
            toPerfControl.Initialize(
                profiles.AcConfigs, profiles.TOTables.ToList(), null);

            // TODO: toPerfControl.Airports = AirportList;
            //toPerfControl.TryLoadState();

            landingPerfControl.InitializeAircrafts(
                profiles.AcConfigs, profiles.LdgTables.ToList(), null);
            //landingPerfControl.Airports = AirportList;
            //landingPerfControl.TryLoadState();
        }

        private void LoadDefaultState()
        {
            ACList.SelectedIndex = 0;
            WtUnitSel_ComboBox.SelectedIndex = 0;

            FinalRsv.Text = "30";
            ContPercentToDest.Text = "5";
            ExtraFuel.Text = "0";
            APUTime.Text = "30";
            TaxiTime.Text = "20";
            HoldTime_TxtBox.Text = "0";

            RouteDisLbl.Text = "";
            RouteDisAltnLbl.Text = "";
        }

        private bool IsRunAsAdministrator()
        {
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            WindowsPrincipal wp = new WindowsPrincipal(wi);

            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async void Startup(object sender, EventArgs e)
        {
            checkRegistry();
            LoadDefaultState();

            if (WtUnitSel_ComboBox.Text == "KG")
            {
                ZFW.Text = Convert.ToString(OperatingEmptyWtKg);
            }
            else
            {
                ZFW.Text = Convert.ToString(Math.Round(OperatingEmptyWtKg * KgLbRatio));
            }

            LoadNavDBUpdateStatusStrip(true);
            ServiceInitializer.Initailize(airportList, wptList);
            //TakeOffPerfCalculation.LoadedData.Load();
            //TODO: LandingPerfCalculation.LoadedData.Load();

            //load previous form states
            formStateManagerFuel = new FormStateSaver(FormStateSaver.PageOfForm.FuelCalculation);
            formStateManagerTO = new FormStateSaver(FormStateSaver.PageOfForm.Takeoff);

            formStateManagerFuel.Load();

            Size = new Size(1280, 900);

            viewChanger = new ViewManager();
            viewChanger.ShowPage(ViewManager.Pages.FuelCalculation);

            startTracksDlAsReq();
            await startWindDlAsReq();
        }

        private static void checkRegistry()
        {
            // Try to check/add registry so that google map works properly. 
            var regChecker = new IeEmulationChecker();

            try
            {
#if DEBUG
                regChecker.DebugRun();
#endif                
                regChecker.Run();
            }
            catch (Exception ex)
            {
                WriteToLog(ex);
            }
        }

        private void startTracksDlAsReq()
        {
            if (appSettings.AutoDLTracks)
            {
                //RouteFinding.Tracks.Interaction.Interactions.SetAllTracksAsync();
                //TODO: add code to start download tracks automatically.
            }
            else
            {
                LblTrackDownloadStatus.Image = Properties.Resources.YellowLight;
                LblTrackDownloadStatus.Text = "Tracks: Not downloaded";
            }
        }

        private async Task startWindDlAsReq()
        {
            if (appSettings.AutoDLWind)
            {
                await downloadWind();
            }
            else
            {
                ShowWindDownloadStatus(WindDownloadStatus.WaitingManualDL);
            }
        }

        private async Task downloadWind()
        {
            ShowWindDownloadStatus(WindDownloadStatus.Downloading);

            try
            {
                await new WindManager().DownloadWindAsync();
                ShowWindDownloadStatus(WindDownloadStatus.Finished);
            }
            catch (Exception ex)
            {
                WriteToLog(ex);
                ShowWindDownloadStatus(WindDownloadStatus.Failed);
            }
        }

        public enum WindDownloadStatus
        {
            Downloading,
            Finished,
            Failed,
            WaitingManualDL
        }

        public void ShowWindDownloadStatus(WindDownloadStatus item)
        {
            switch (item)
            {
                case WindDownloadStatus.Downloading:
                    WindDownloadStatus_Lbl.Text = "Downloading lastest wind ...";
                    WindDownloadStatus_Lbl.Image = null;
                    break;

                case WindDownloadStatus.Finished:
                    WindDownloadStatus_Lbl.Text = "Lastest wind ready";
                    WindDownloadStatus_Lbl.Image = Properties.Resources.GreenLight;
                    break;

                case WindDownloadStatus.Failed:
                    WindDownloadStatus_Lbl.Text = "Failed to download wind data";
                    WindDownloadStatus_Lbl.Image = Properties.Resources.RedLight;
                    break;

                case WindDownloadStatus.WaitingManualDL:
                    WindDownloadStatus_Lbl.Text = "Click here to download wind data";
                    WindDownloadStatus_Lbl.Image = Properties.Resources.YellowLight;
                    break;
            }
        }

        public void LoadNavDBUpdateStatusStrip(bool startingApp)
        {
            try
            {
                //if success, update the status strip

                var t = OptionsForm.AiracCyclePeriod(appSettings.NavDataLocation);
                //this returns, for example, (1407,26JUN23JUL/14)

                bool expired = !AiracTools.AiracValid(t.Item2);
                if (expired)
                {
                    StatusLabel1.Image = Properties.Resources.YellowLight;
                    StatusLabel1.Text = "AIRAC: " + t.Item1 + " (" + t.Item2 + ") - Expired";
                }
                else
                {
                    StatusLabel1.Image = Properties.Resources.GreenLight;
                    StatusLabel1.Text = "AIRAC: " + t.Item1 + " (" + t.Item2 + ")";
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex);
                // Open the option window
                StatusLabel1.Image = Properties.Resources.RedLight;
                StatusLabel1.Text = "Failed to load Nav DB.";

                if (startingApp)
                {
                    showOptionsForm();
                }
            }
        }

        private void MissedAppFuel_TextChanged(object sender, EventArgs e)
        {
            FuelReport_TxtBox.Text = "";
        }

        private void TailwindToAltn_TextChanged(object sender, EventArgs e)
        {
            FuelReport_TxtBox.Text = "";
        }

        private void updateACWtProperty()
        {
            switch (ACList.Text)
            {
                case "737-600":
                    OperatingEmptyWtKg = 36378;
                    MaxZfwKg = 51483;
                    MissedAppFuelKG = 130;
                    break;
                case "737-700":
                    OperatingEmptyWtKg = 37648;
                    MaxZfwKg = 54658;
                    MissedAppFuelKG = 130;
                    break;
                case "737-800":
                    OperatingEmptyWtKg = 41413;
                    MaxZfwKg = 61689;
                    MissedAppFuelKG = 130;
                    break;
                case "737-900":
                    OperatingEmptyWtKg = 42901;
                    MaxZfwKg = 63639;
                    MissedAppFuelKG = 130;
                    break;
                case "777-200LR":
                    OperatingEmptyWtKg = 145150;
                    MaxZfwKg = 209106;
                    MissedAppFuelKG = 300;
                    //subject to change
                    break;
                case "777F":
                    OperatingEmptyWtKg = 144400;
                    MaxZfwKg = 248115;
                    MissedAppFuelKG = 300;
                    //subject to change
                    break;
            }
        }

        private void AC_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            FuelReport_TxtBox.Text = "";
            updateACWtProperty();

            double zfwKg = 0;
            double.TryParse(ZFW.Text, out zfwKg);

            if (WtUnitSel_ComboBox.Text == "KG")
            {
                MissedAppFuel.Text = Convert.ToString(MissedAppFuelKG);

                if (zfwKg > MaxZfwKg || zfwKg < OperatingEmptyWtKg)
                {
                    ZFW.Text = Convert.ToString(OperatingEmptyWtKg);
                }
            }
            else
            {
                zfwKg *= LbKgRatio;
                MissedAppFuel.Text = Convert.ToString(Math.Round(MissedAppFuelKG * KgLbRatio));

                if (zfwKg > MaxZfwKg || zfwKg < OperatingEmptyWtKg)
                {
                    ZFW.Text = Convert.ToString(Math.Round(OperatingEmptyWtKg * KgLbRatio));
                }

            }
            checkZfwInRange();
        }


        private void checkZfwInRange()
        {
            double ZFWKg = 0;

            if (WtUnitSel_ComboBox.Text == "KG")
            {
                ZFWKg = Convert.ToDouble(ZFW.Text);
            }
            else
            {
                ZFWKg = Convert.ToDouble(ZFW.Text) * LbKgRatio;
            }

            if (ZFWKg > MaxZfwKg || ZFWKg < OperatingEmptyWtKg)
            {
                ZFW.ForeColor = Color.Red;
            }
            else
            {
                ZFW.ForeColor = Color.Green;
            }

        }

        private void ZFW_TextChanged(object sender, EventArgs e)
        {
            checkZfwInRange();
            FuelReport_TxtBox.Text = "";
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void AboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            new about().ShowDialog();
        }

        private void OptionsToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            showOptionsForm();
        }

        private void showOptionsForm()
        {
            var frm = new OptionsForm();
            frm.Init(appSettings);
            frm.AppSettingChanged += (sender, e) =>
              {
                  appSettings = frm.AppSettings;
              };
            frm.ShowDialog();
        }

        private void setHandCusor(object sender, EventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void setDefaultCursor(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
        }

        private void StatusLabel1_Click(object sender, EventArgs e)
        {
            showOptionsForm();
        }

        private void ShowTO_Btn_Click(object sender, EventArgs e)
        {
            if (takeoffControlInitialized == false)
            {
                takeoffControlInitialized = true;
                //toPerfControl.InitializeAircrafts(null, null);//TODO: load the data here.
                toPerfControl.Airports = airportList;
                toPerfControl.TryLoadState();
            }
            viewChanger.ShowPage(ViewManager.Pages.TakeoffPerf);
        }

        private void ShowLDG_Btn_Click(object sender, EventArgs e)
        {
            if (landingControlInitialized == false)
            {
                landingControlInitialized = true;
                //landingPerfControl.InitializeAircrafts();
                landingPerfControl.Airports = airportList;
                landingPerfControl.TryLoadState();
            }
            viewChanger.ShowPage(ViewManager.Pages.LandingPerf);
        }

        private void ShowAPData_Btn_Click(object sender, EventArgs e)
        {
            if (!InitializeFinished_AirportDataFinder)
            {
                AirportDataFinder_Load();
            }

            viewChanger.ShowPage(ViewManager.Pages.Misc);
            UpdateComboBoxList();
        }

        private bool tabRefreshed = false;
        //last time the descend forcast is generated for this airport
        private string DesForcastAirportIcao = "";

        private string GenDesForcastString(string icao)
        {
            var latlon = airportList.AirportLatlon(icao);
            int[] FLs = { 60, 90, 120, 180, 240, 300, 340, 390, 440, 490 };
            var forcastGen = new DescendForcastGenerator(latlon.Lat, latlon.Lon, FLs);

            Wind[] w = forcastGen.Generate();
            var result = new StringBuilder();

            for (int i = 0; i < FLs.Length; i++)
            {
                result.AppendLine("        FL" + FLs[i].ToString().PadLeft(3, '0') + "   " + w[i].DirectionString() +
                    "/" + (int)w[i].Speed);
            }

            return result.ToString();
        }

        private async void Refresh_TabControl(object sender, EventArgs e)
        {
            if (TabControl1.SelectedIndex == 1 && !tabRefreshed)
            {
                await Task.Factory.StartNew(() => metarMonitor.updateOrig(OrigTxtBox.Text));
                await Task.Factory.StartNew(() => metarMonitor.updateDest(DestTxtBox.Text));
                await Task.Factory.StartNew(() => metarMonitor.updateAltn(AltnTxtBox.Text));

                updateMTDisplay();
            }
            else if (TabControl1.SelectedIndex == 2 & DesForcastAirportIcao != DestTxtBox.Text)
            {
                try
                {
                    DesForcast_RTextBox.Text = Environment.NewLine + Environment.NewLine + Environment.NewLine + "           Refreshing ...";
                    Label86.Text = "DEST / " + DestTxtBox.Text;
                    DesForcastAirportIcao = DestTxtBox.Text;

                    DesForcast_RTextBox.Text = await Task.Factory.StartNew(() => GenDesForcastString(DestTxtBox.Text));
                }
                catch (Exception ex)
                {
                    WriteToLog(ex);
                    DesForcast_RTextBox.Text = Environment.NewLine + Environment.NewLine + Environment.NewLine + "     Unable to get descend forcast for " + DestTxtBox.Text;
                }
            }
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://flightaware.com/statistics/ifr-route/");
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://rfinder.asalink.net/free/");
        }

        private void LinkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://aviationweather.gov/adds/metars/");
        }

        private void LinkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://aviationweather.gov/webiffdp/page/public?name=iffdp_main");
        }

        private void LinkLabel5_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://aviationweather.gov/iffdp/sgwx");
        }

        private void LinkLabel6_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://www.notams.faa.gov/dinsQueryWeb/");
        }

        private void LinkLabel7_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.faa.gov/air_traffic/flight_info/aeronav/digital_products/");
        }

        private void LinkLabel8_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.ead.eurocontrol.int/eadcms/eadsite/index.php.html");
        }


        private void CloseMain(object sender, CancelEventArgs e)
        {
            if (appSettings.PromptBeforeExit)
            {
                // Initializes variables to pass to the MessageBox.Show method. 

                string Message = "Exit the application?";
                string Caption = "";
                var Buttons = MessageBoxButtons.YesNo;
                var Icon = MessageBoxIcon.Question;

                //Displays the MessageBox
                var Result = MessageBox.Show(Message, Caption, Buttons, Icon);

                // Gets the result of the MessageBox display. 
                if (Result == DialogResult.No)
                {
                    // Do not exit the app.
                    e.Cancel = true;
                }
            }
        }

        #region "RouteGen"

        private static string PMDGrteFile;
       
        private List<string> getSidStarList(ComboBox CBox)
        {
            var sidStar = new List<string>();

            if (CBox.Text == "AUTO")
            {
                foreach (var i in CBox.Items)
                {
                    string s = Convert.ToString(i);

                    if (s != "AUTO")
                    {
                        sidStar.Add(s);
                    }
                }
            }
            else if (CBox.Text != "NONE")
            {
                sidStar.Add(CBox.Text);
            }

            return sidStar;
        }

        private void FindRouteToDestBtn_Click(object sender, EventArgs e)
        {
            List<string> sid = getSidStarList(OrigSidComboBox);
            List<string> star = getSidStarList(DestStarComboBox);

            RouteToDest = new RouteGroup(new RouteFinderFacade(wptList, airportList, appSettings.NavDataLocation)
                                           .FindRoute(OrigTxtBox.Text, OrigRwyComboBox.Text, sid,
                                                      DestTxtBox.Text, DestRwyComboBox.Text, star),
                                           TracksInUse);

            var route = RouteToDest.Expanded;

            PMDGrteFile = new PmdgProvider(route, airportList)
                .GetExportText();

            RouteDisplayRichTxtBox.Text = route.ToString(false, false);
            UpdateRouteDistanceLbl(RouteDisLbl, route);
        }

        public static void UpdateRouteDistanceLbl(Label lbl, Route route)
        {
            double totalDis = route.GetTotalDistance();
            int disInt = Doubles.RoundToInt(totalDis);
            double directDis =
                route.FirstWaypoint.DistanceFrom(route.LastWaypoint);
            double percentDiff = (totalDis - directDis) / directDis * 100;
            string diffStr = percentDiff.ToString("0.0");

            lbl.Text = $"Total Dis: {disInt} NM (+{diffStr}%)";
        }

        private static int computeTailWind(TailWindCalcOptions para, int tas, int Fl)
        {
            if (para == TailWindCalcOptions.OrigToDest)
            {
                return WindAloft.Utilities.AvgTailWind(RouteToDest.Expanded, Fl, tas);
            }
            else
            {
                return WindAloft.Utilities.AvgTailWind(RouteToAltn.Expanded, Fl, tas);
            }
        }

        private void GenRteAltnBtnClick(object sender, EventArgs e)
        {
            // Get a list of sids
            var sids = SidHandlerFactory.GetHandler(DestTxtBox.Text, appSettings.NavDataLocation, wptList, wptList.GetEditor(), airportList)
                                        .GetSidList(DestRwyComboBox.Text);
            var starAltn = getSidStarList(AltnStarComboBox);

            RouteToAltn = new RouteGroup(new RouteFinderFacade(wptList, airportList, appSettings.NavDataLocation)
                                           .FindRoute(DestTxtBox.Text, DestRwyComboBox.Text, sids,
                                                      AltnTxtBox.Text, AltnRwyComboBox.Text, starAltn),
                                           TracksInUse);

            var route = RouteToAltn.Expanded;

            RouteDisplayAltnRichTxtBox.Text = route.ToString(false, false);
            UpdateRouteDistanceLbl(RouteDisAltnLbl, route);
        }

        private void ExportRouteFiles()
        {
            var cmds = appSettings.ExportCommands.Values;
            var writer = new FileExporter(RouteToDest.Expanded, airportList, cmds);

            var reports = writer.Export();
            showReports(reports);
        }

        private static void showReports(IEnumerable<FileExporter.Status> reports)
        {
            if (reports.Count() == 0)
            {
                MessageBox.Show(
                    "No route file to be exported. " +
                    "Please select select export settings in options page.",
                    "",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                var msg = new StringBuilder();
                var success = reports.Where(r => r.Successful);

                if (success.Count() > 0)
                {
                    msg.AppendLine(
                        $"{success.Count()} company route(s) exported:");

                    foreach (var i in success)
                    {
                        msg.AppendLine(i.FilePath);
                    }
                }

                var errors = reports.Where(r => r.Successful == false);

                if (errors.Count() > 0)
                {
                    msg.AppendLine(
                        $"\n\nFailed to export {errors.Count()} file(s) into:");

                    foreach (var j in errors)
                    {
                        msg.AppendLine(j.FilePath);
                    }
                }

                var icon =
                    errors.Count() > 0 ?
                    MessageBoxIcon.Warning :
                    MessageBoxIcon.Information;

                MessageBox.Show(
                    msg.ToString(),
                    "",
                    MessageBoxButtons.OK,
                    icon);
            }
        }
        
        private void Analyze_RteToDest_Click(object sender, EventArgs e)
        {
            //TODO: Need better exception message for AUTO, RAND commands
            try
            {
                RouteDisplayRichTxtBox.Text = RouteDisplayRichTxtBox.Text.ToUpper();

                RouteToDest =
                    new RouteGroup(
                        RouteAnalyzerFacade.AnalyzeWithCommands(
                            RouteDisplayRichTxtBox.Text,
                            OrigTxtBox.Text,
                            OrigRwyComboBox.Text,
                            DestTxtBox.Text,
                            DestRwyComboBox.Text,
                            appSettings.NavDataLocation,
                            airportList,
                            wptList),
                        TracksInUse);

                var route = RouteToDest.Expanded;

                PMDGrteFile = new PmdgProvider(route, airportList)
                .GetExportText();

                RouteDisplayRichTxtBox.Text = route.ToString(false, false);
                UpdateRouteDistanceLbl(RouteDisLbl, route);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void Button8_Click(object sender, EventArgs e)
        {
            viewChanger.ShowPage(ViewManager.Pages.FuelCalculation);
        }

        #endregion

        #region "TakeOffPart"

        private bool takeoffControlInitialized = false;
        private string AC_Req;
        private WeightUnit TOWT_Req_Unit;

        private int TOWT_Req;

        #endregion

        #region "LandingPart"

        private bool landingControlInitialized = false;

        #endregion
        //========================================= Misc Part ==========================================

        bool InitializeFinished_AirportDataFinder = false;

        metar_monitor metarMonitor = new metar_monitor();

        public class metar_monitor
        {
            public string orig;
            public string dest;
            public string altn;
            public string orig_mt;
            public string dest_mt;
            public string altn_mt;
            //orig = icao of origin
            //orig_mt = metar/taf of orig.

            public metar_monitor()
            {
                orig = "";
                dest = "";
                altn = "";
                orig_mt = "";
                dest_mt = "";
                altn_mt = "";
            }

            public void updateOrig(string new_orig)
            {
                orig = new_orig;
                orig_mt = MetarDownloader.TryGetMetarTaf(orig);
            }

            public void updateDest(string new_dest)
            {
                dest = new_dest;
                dest_mt = MetarDownloader.TryGetMetarTaf(dest);
            }

            public void updateAltn(string new_altn)
            {
                altn = new_altn;
                altn_mt = MetarDownloader.TryGetMetarTaf(altn);
            }

            public void refreshAll()
            {
                orig_mt = MetarDownloader.TryGetMetarTaf(orig);
                dest_mt = MetarDownloader.TryGetMetarTaf(dest);
                altn_mt = MetarDownloader.TryGetMetarTaf(altn);
            }

        }

        private void UpdateAll_Btn_Click(object sender, EventArgs e)
        {
            metarMonitor.updateOrig(OrigTxtBox.Text);
            metarMonitor.updateDest(DestTxtBox.Text);
            metarMonitor.updateAltn(AltnTxtBox.Text);

            metarMonitor.refreshAll();
            updateMTDisplay();
        }

        public void updateMTDisplay()
        {
            RichTextBox2.Text = metarMonitor.orig_mt + metarMonitor.dest_mt + metarMonitor.altn_mt;
        }

        private void DownloadMetar_Btn_Click(object sender, EventArgs e)
        {
            MetarToFindTxtBox.Text = MetarToFindTxtBox.Text.ToUpper();
            RichTextBox1.Text = MetarDownloader.TryGetMetarTaf(MetarToFindTxtBox.Text);
        }

        private void AirportDataFinder_Load()
        {
            airportMapControl.Initialize(airportList);
            airportMapControl.BrowserEnabled = true;
            UpdateComboBoxList();

            InitializeFinished_AirportDataFinder = true;
        }

        public void UpdateComboBoxList()
        {
            var icaoList = airportMapControl.icaoComboBox.Items;

            icaoList.Clear();
            icaoList.Add(OrigTxtBox.Text);
            icaoList.Add(DestTxtBox.Text);
            icaoList.Add(AltnTxtBox.Text);
        }

        private void DrawRouteToDest()
        {
            if (RouteToDest == null)
            {
                return;
            }

            StringBuilder GoogleMapDrawRoute = RouteDrawing.MapDrawString(RouteToDest.Expanded, MapDisWebBrowser.Width - 20, MapDisWebBrowser.Height - 30);

            var mapStr = GoogleMapDrawRoute.ToString();

            if (MapDisWebBrowser.DocumentText != mapStr)
            {
                MapDisWebBrowser.DocumentText = mapStr;
            }

        }

        private void WtManage_Btn_Click(object sender, EventArgs e)
        {
            new WtManagement().ShowDialog();
        }

        private void FindAltn_Btn_Click(object sender, EventArgs e)
        {
            var altnFrm = new FindAltnForm();
            altnFrm.Initialize(airportList);
            altnFrm.ShowDialog();
        }
        
        private void ShowMap_Btn_Click(object sender, EventArgs e)
        {
            DrawRouteToDest();
            MainWin_TablessControl.SelectedIndex = 6;
        }

        private void Return_Btn_Click(object sender, EventArgs e)
        {
            MainWin_TablessControl.SelectedIndex = 0;
        }

        private void Label42_Click(object sender, EventArgs e)
        {
            viewChanger.ShowPage(ViewManager.Pages.RouteTools);
        }

        private void moveBtnDown(int amount, Button btn)
        {
            Point pt = btn.Location;
            pt.Y += amount;
            btn.Location = pt;
        }

        private void Return2_Btn_Click(object sender, EventArgs e)
        {
            MainWin_TablessControl.SelectedIndex = 0;
        }

        private void WtUnitSel_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            double missedAppFuel;
            double extra;
            double zfw;

            if (string.IsNullOrEmpty(ZFW.Text))
            {
                ZFW.Text = "0";
            }

            if (double.TryParse(MissedAppFuel.Text, out missedAppFuel) &&
                    double.TryParse(ExtraFuel.Text, out extra) && double.TryParse(ZFW.Text, out zfw))
            {
                FuelReport_TxtBox.Text = "";

                if (WtUnitSel_ComboBox.Text == "KG")
                {
                    Label11.Text = "KG";
                    Label13.Text = "KG";
                    Label34.Text = "KG";

                    if (string.IsNullOrEmpty(ZFW.Text))
                    {
                        ZFW.Text = "0";
                    }

                    MissedAppFuel.Text = Convert.ToString(Math.Round(missedAppFuel * LbKgRatio));
                    ExtraFuel.Text = Convert.ToString(Math.Round(extra * LbKgRatio));
                    ZFW.Text = Convert.ToString(Math.Round(zfw * LbKgRatio));

                }
                else
                {
                    FuelReport_TxtBox.Text = "";

                    Label11.Text = "LB";
                    Label13.Text = "LB";
                    Label34.Text = "LB";

                    MissedAppFuel.Text = Convert.ToString(Math.Round(missedAppFuel * KgLbRatio));
                    ExtraFuel.Text = Convert.ToString(Math.Round(extra * KgLbRatio));
                    ZFW.Text = Convert.ToString(Math.Round(zfw * KgLbRatio));
                }
            }
        }

        private void FuelReportView_Btn_Click(object sender, EventArgs e)
        {
            viewChanger.ShowPage(ViewManager.Pages.FuelReport);
        }

        private TracksForm trkFormInstance()
        {
            return (TracksForm)Application.OpenForms.Cast<Form>().Where(x => x is TracksForm).FirstOrDefault();
        }

        private void LblTrackDownloadStatus_Click(object sender, EventArgs e)
        {
            var trkForm = trkFormInstance();

            if (trkForm == null)
            {
                trkForm = new TracksForm();
            }

            trkForm.Show();

        }

        private async void WindDownloadStatus_Lbl_Click(object sender, EventArgs e)
        {
            await downloadWind();
        }

        private void ExportBtn_Click(object sender, EventArgs e)
        {
            ExportRouteFiles();
        }

    }
}
