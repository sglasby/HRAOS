//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Diagnostics;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;

//namespace OpenGL_Control_experiment
//{
//    public class OpenGLControlHandler
//    {
//        Stopwatch sw = new Stopwatch(); // available to all event handlers

//        void Application_Idle(object sender, EventArgs e)
//        {
//            double milliseconds = ComputeTimeSlice();
//            Accumulate(milliseconds);
//            Animate(milliseconds);
//        }

//        float rotation = 0;
//        private void Animate(double milliseconds)
//        {
//            float deltaRotation = (float) milliseconds / 20.0f;
//            rotation += deltaRotation;
//            glControl.Invalidate();
//        }

//        double accumulator = 0;
//        int idleCounter = 0;
//        private void Accumulate(double milliseconds)
//        {
//            idleCounter++;
//            accumulator += milliseconds;
//            if (accumulator > 1000)
//            {
//                label1.Text = idleCounter.ToString();
//                accumulator -= 1000;
//                idleCounter = 0; // don't forget to reset the counter!
//            }
//        }

//        private double ComputeTimeSlice()
//        {
//            sw.Stop();
//            double timeslice = sw.Elapsed.TotalMilliseconds;
//            sw.Reset();
//            sw.Start();
//            return timeslice;
//        }
//    }
//}
