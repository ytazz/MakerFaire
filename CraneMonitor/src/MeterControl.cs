﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace MeterDisplay
{
    public class MeterControl
    {
        public BoeingMeter Meter { get; set; }
        public MeasureObj MeasureObj
        {
            get { return measurement; }
            set
            {
                measurement = value;
                Initialize();
            }
        }
        public int MaxTimeIndex
        {
            set { delta_x = 1.0f / value; }
        }
        public bool IsEnable
        {
            set
            {
                is_enable = value;
                if (is_enable)
                {
                    Meter.Visibility = Visibility.Visible;
                    Meter.IsEnabled = true;
                }
                else
                {
                    Meter.Visibility = Visibility.Hidden;
                    Meter.IsEnabled = false;
                    measurement = null; // release the resources
                }
            }
            get { return is_enable; }
        }
        private bool is_enable = true;
        private MeasureObj measurement = null;
        private float prev_value;
        private float next_value;
        private float prev_norm_value;
        private float next_norm_value;
        private float delta_y;
        private float delta_ny;
        private float delta_x;
        private float x;

        private void Initialize()
        {
            if (measurement != null)
            {
                measurement.InitializeMeterFormat(Meter);
                measurement.Update();
                prev_value = next_value = measurement.GetValue();
                prev_norm_value = next_norm_value = measurement.GetNormValue();
            }
        }

        public void UpdateData()
        {
            if (measurement != null)
            {
                measurement.Update();

                prev_value = next_value;
                next_value = measurement.GetValue();
                delta_y = next_value - prev_value;

                prev_norm_value = next_norm_value;
                next_norm_value = measurement.GetNormValue();
                delta_ny = next_norm_value - prev_norm_value;
            }
        }

        public void UpdateSmooth(int time_index)
        {
            if (measurement != null)
            {
                if (time_index == 0)
                {
                    Meter.MeterValue = prev_value;
                    Meter.MeterNormValue = prev_norm_value;
                    SetMeterFillColor(Meter.MeterNormValue);
                    Meter.UpdateMeter();
                    x = 0;
                }
                else
                {
                    float z = 1.0f - x;
                    float y = 1.0f - z * z;
                    Meter.MeterValue = prev_value + delta_y * y;
                    Meter.MeterNormValue = prev_norm_value + delta_ny * y;
                    SetMeterFillColor(Meter.MeterNormValue);
                    Meter.UpdateMeter();
                }
                x += delta_x;
            }
        }

        public void UpdateDirect()
        {
            if (measurement != null)
            {
                Meter.MeterValue = next_value;
                Meter.MeterNormValue = next_norm_value;
                SetMeterFillColor(Meter.MeterNormValue);
                Meter.UpdateMeter();
            }
        }

        // ------------------------------------------------------------

        private int num_thresholds = 0;
        private float[] thresholds = null;
        private Brush[] brushes = null;
        private Brush[] scale_brushes = null;
        private float threshold_scale_len = 0.15f;

        public int NumThresholds
        {
            set {
                Meter.RemoveScales();
                if(value > 0 && value != num_thresholds)
                {
                    thresholds = new float[value];
                    for (int i = 0; i < thresholds.Count(); i++)
                        thresholds[i] = 0;
                    brushes = new Brush[value + 1];
                    for (int i = 0; i < brushes.Count(); i++)
                        brushes[i] = null;
                    scale_brushes = new Brush[value];
                    for (int i = 0; i < scale_brushes.Count(); i++)
                        brushes[i] = null;
                }
                num_thresholds = value;
            }
            get { return num_thresholds; }
        }
        public float ThresholdScaleLen
        {
            set { threshold_scale_len = value; }
            get { return threshold_scale_len; }
        }

        public void SetThreshold(int n, float th)
        {
            thresholds[n] = th;
        }
        public void SetThresholdBrush(int n, Brush brush)
        {
            brushes[n] = brush;
        }
        public void SetThresholdScaleBrush(int n, Brush brush)
        {
            scale_brushes[n] = brush;
        }
        public void RedrawScales()
        {
            Meter.RemoveScales();
            if (threshold_scale_len == 0.0f) return;
            for(int i = 0; i < num_thresholds; i++)
            {
                if (scale_brushes[i] != null)
                {
                    Meter.AddScale(thresholds[i], scale_brushes[i], 1.0f, 1.0f - threshold_scale_len);
                }
            }
        }
        public void SetMeterFillColor(float val)
        {
            if (num_thresholds == 0) return;
            int n = 0;
            for (int i = 0; i < num_thresholds; i++)
                if (thresholds[i] <= val)
                    n = i + 1;
            Meter.FillBrush = brushes[n];
        }
    }

    public abstract class MeasureObj
    {
        public abstract void InitializeMeterFormat(BoeingMeter meter);
        public abstract void Update();
        public abstract float GetValue();
        public abstract float GetNormValue();
    }

    public delegate float GetRealValue();

    public class MeasurePercent : MeasureObj
    {
        GetRealValue method;
        float value;
        public MeasurePercent(GetRealValue x) { method = x; }
        public override void Update() { value = method(); }
        public override void InitializeMeterFormat(BoeingMeter meter)
        {
            meter.ValueFormat = "{0:0}%";
        }
        public override float GetValue()
        {
            return 100.0f * value;
        }
        public override float GetNormValue()
        {
            return value;
        }
    };
    public class MeasurePos : MeasureObj
    {
        GetRealValue method;
        float value;
        float min_value = float.MaxValue;
        float max_value = float.MinValue;

        public MeasurePos(GetRealValue x) { method = x; }
        public override void Update()
        {
            value = method();
            if (value < min_value) min_value = value;
            if (value > max_value) max_value = value;
        }
        public override void InitializeMeterFormat(BoeingMeter meter)
        {
            meter.ValueFormat = "{0:0}";
        }
        public override float GetValue()
        {
            return value;
        }
        public override float GetNormValue()
        {
            return (max_value == min_value) ? 0 : (value - min_value) / (max_value - min_value);
        }
    };

}
