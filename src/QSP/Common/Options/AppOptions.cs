using QSP.LibraryExtension.XmlSerialization;
using QSP.RouteFinding.FileExport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static QSP.LibraryExtension.XmlSerialization.SerializationHelper;
using static QSP.Utilities.ExceptionHelpers;

namespace QSP.Common.Options
{
    public class AppOptions
    {
        public string NavDataLocation { get; private set; }
        public bool PromptBeforeExit { get; private set; }
        public bool AutoDLTracks { get; private set; }
        public bool AutoDLWind { get; private set; }
        public bool EnableWindOptimizedRoute { get; private set; }
        public bool HideDctInRoute { get; private set; }
        public bool ShowTrackIdOnly { get; private set; }
        public bool AutoUpdate { get; private set; }
        public IReadOnlyDictionary<string, string> SimulatorPaths { get; private set; }
        public IReadOnlyDictionary<string, ExportCommand> ExportCommands { get; private set; }

        public AppOptions(
            string NavDataLocation,
            bool PromptBeforeExit,
            bool AutoDLTracks,
            bool AutoDLWind,
            bool EnableWindOptimizedRoute,
            bool HideDctInRoute,
            bool ShowTrackIdOnly,
            bool AutoUpdate,
            IReadOnlyDictionary<string, string> SimulatorPaths,
            IReadOnlyDictionary<string, ExportCommand> ExportCommands)
        {
            this.NavDataLocation = NavDataLocation;
            this.PromptBeforeExit = PromptBeforeExit;
            this.AutoDLTracks = AutoDLTracks;
            this.AutoDLWind = AutoDLWind;
            this.EnableWindOptimizedRoute = EnableWindOptimizedRoute;
            this.HideDctInRoute = HideDctInRoute;
            this.ShowTrackIdOnly = ShowTrackIdOnly;
            this.AutoUpdate = AutoUpdate;
            this.SimulatorPaths = SimulatorPaths;
            this.ExportCommands = ExportCommands;
        }

        public static AppOptions Default => new AppOptions(
            Path.GetFullPath("../NavData"),
            true,
            true,
            true,
            true,
            false,
            false,
            true,
            new Dictionary<string, string>(),
            new Dictionary<string, ExportCommand>());

        public class Serializer : IXSerializer<AppOptions>
        {
            public XElement Serialize(AppOptions a, string name)
            {
                return new XElement(name, new XElement[]
                {
                    a.NavDataLocation.Serialize("DatabasePath"),
                    a.PromptBeforeExit.Serialize("PromptBeforeExit"),
                    a.AutoDLTracks.Serialize("AutoDLTracks"),
                    a.AutoDLWind.Serialize("AutoDLWind"),
                    a.EnableWindOptimizedRoute.Serialize("WindOptimizedRoute"),
                    a.HideDctInRoute.Serialize("HideDctInRoute"),
                    a.ShowTrackIdOnly.Serialize("ShowTrackIdOnly"),
                    a.AutoUpdate.Serialize("AutoUpdate"),
                    a.SimulatorPaths.Serialize("SimulatorPaths"),
                    a.ExportCommands.ToDictionary(kv => kv.Key,
                                                  kv => kv.Value.Serialize("command").ToString())
                                    .Serialize("ExportCommands")
                });
            }

            public AppOptions Deserialize(XElement item)
            {
                var d = Default;

                Action[] actions =
                {
                    () => d.NavDataLocation = item.GetString("DatabasePath"),
                    () => d.PromptBeforeExit = item.GetBool("PromptBeforeExit"),
                    () => d.AutoDLTracks = item.GetBool("AutoDLTracks"),
                    () => d.AutoDLWind = item.GetBool("AutoDLWind"),
                    () => d.EnableWindOptimizedRoute =
                            item.GetBool("WindOptimizedRoute"),
                    () => d.HideDctInRoute = item.GetBool("HideDctInRoute"),
                    () => d.ShowTrackIdOnly = item.GetBool("ShowTrackIdOnly"),
                    () => d.AutoUpdate = item.GetBool("AutoUpdate"),
                    () => d.SimulatorPaths=item.GetDict("SimulatorPaths"),
                    () => d.ExportCommands = item.GetDict("ExportCommands")
                        .ToDictionary(kv => kv.Key,
                                      kv => ExportCommand.Deserialize(XElement.Parse(kv.Value)))
                };

                foreach (var a in actions)
                {
                    IgnoreException(a);
                }

                return d;
            }
        }

    }
}
