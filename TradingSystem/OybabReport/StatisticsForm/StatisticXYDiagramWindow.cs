﻿using ComponentFactory.Krypton.Toolkit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oybab.DAL;
using System.IO;
using DevExpress.XtraCharts;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Oybab.Report.Model;

namespace Oybab.Report.StatisticsForm
{
    public sealed partial class StatisticXYDiagramWindow : KryptonForm
    {
        private Font TheFont = null;

        XYDiagram Diagram { get { return chartControl1.Diagram as XYDiagram; } }
        AxisBase AxisX { get { return Diagram != null ? Diagram.AxisX : null; } }
        private Series series1 { get { return chartControl1.Series[0]; } }


        public StatisticXYDiagramWindow(List<RecordTime> records, StatisticModel Model)
        {
            InitializeComponent();

            this.Text = Model.Title;


            Assembly asm = Assembly.LoadFrom(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Res.dll"));
            this.Icon = new Icon(asm.GetManifestResourceStream(@"Oybab.Res.Resources.Images.PC.Statistic.ico"));

            // This line of code is generated by Data Source Configuration Wizard
            LoadPoints(series1, records);
            
            series1.Name = Model.Title;

            TimeSpan offset = (DateTime)AxisX.VisualRange.MaxValue - (DateTime)AxisX.VisualRange.MinValue;
            offset = new TimeSpan((long)(offset.Ticks / 4));
            AxisX.VisualRange.SetMinMaxValues((DateTime)AxisX.VisualRange.MinValue + offset, (DateTime)AxisX.VisualRange.MaxValue - offset);

            AxisX.DateTimeScaleOptions.AggregateFunction = AggregateFunction.Sum;
            // 设置时间
            DateTime maxTime = records.Max(x=>x.Time);
            DateTime minTime = records.Min(x=>x.Time);



            if (maxTime.Year == minTime.Year && maxTime.DayOfYear == minTime.DayOfYear)
            {
                AxisX.DateTimeScaleOptions.MeasureUnit = DateTimeMeasureUnit.Hour;

                if (maxTime.Hour == minTime.Hour)
                {
                    AxisX.DateTimeScaleOptions.MeasureUnit = DateTimeMeasureUnit.Minute;

                    if (maxTime.Minute == minTime.Minute)
                    {
                        AxisX.DateTimeScaleOptions.MeasureUnit = DateTimeMeasureUnit.Second;
                    }
                    
                }
            }
            else if ((maxTime - minTime).TotalDays <= 1)
            {
                AxisX.DateTimeScaleOptions.MeasureUnit = DateTimeMeasureUnit.Hour;
            }


            TheFont = Model.Font;


            if (Model.EnableAntialiasing)
            {
                

                series1.CrosshairLabelPattern = "{V}:{A}";
                ((XYDiagram)chartControl1.Diagram).AxisX.Label.Font = TheFont;
                chartControl1.Legend.Font = TheFont;
                chartControl1.CustomDrawCrosshair += chartControl1_CustomDrawCrosshair;

                chartControl1.Legend.Antialiasing = true;
                ((XYDiagram)chartControl1.Diagram).AxisX.Label.EnableAntialiasing = DevExpress.Utils.DefaultBoolean.True;

            }
            
                

        }


        private void chartControl1_CustomDrawCrosshair(object sender, CustomDrawCrosshairEventArgs e)
        {

            foreach (CrosshairElement element in e.CrosshairElementGroups.SelectMany(eg => eg.CrosshairElements))
            {
                element.LabelElement.Font = TheFont;

            }


        }


        void LoadPoints(Series series, List<RecordTime> timeList)
        {
            if (series != null && timeList != null && timeList.Count > 0)
            {
                foreach (var item in timeList)
                {
                    series.Points.Add(new SeriesPoint(item.Time, item.Price));
                }

                series.Points.EndUpdate();
            }
        }


    }
}
