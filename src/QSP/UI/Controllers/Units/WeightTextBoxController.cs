﻿using QSP.Utilities.Units;
using System;
using System.Windows.Forms;
using static QSP.AviationTools.Constants;

namespace QSP.UI.Controllers.Units
{
    // Use this abstraction so that there is no need to worry about 
    // weight units.
    // Sync the weight unit with the text in TextBox.
    public class WeightTextBoxController
    {
        public TextBox TxtBox { get; private set; }
        private string format;
        private WeightUnit _unit;

        public WeightTextBoxController(
            TextBox txtBox,
            string format = "F0",             // Display as integer
            WeightUnit unit = WeightUnit.KG)
        {
            this.TxtBox = txtBox;
            this.format = format;
            this._unit = unit;
        }

        /// <exception cref="InvalidOperationException"></exception>
        public double GetWeightKg()
        {
            double num;

            if (double.TryParse(TxtBox.Text.Trim(), out num))
            {
                if (_unit == WeightUnit.KG)
                {
                    return num;
                }
                else
                {
                    return num * LbKgRatio;
                }
            }

            throw new InvalidOperationException();
        }

        public void SetWeight(double weightKg)
        {
            double num = weightKg;

            if (_unit == WeightUnit.LB)
            {
                // Display in LB.
                num *= KgLbRatio;
            }

            TxtBox.Text = num.ToString(format);
        }

        public WeightUnit Unit
        {
            get
            {
                return _unit;
            }

            set
            {
                if (value == _unit)
                {
                    return;
                }

                _unit = value;
                var wt = GetWeightKg();
                SetWeight(wt);
            }
        }
    }
}
