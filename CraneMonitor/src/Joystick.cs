using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace CraneMonitor
{
    public class Joystick
    {
        public enum Direction {
            Up,
            Down,
            Right,
            Left,
            Stop
        };
        
        public SharpDX.DirectInput.Joystick dxJoystick;
        public double[] axis;  //< axis position [-1.0, 1.0]
        public bool[] button;   //< button state {0, 1}

        public Joystick()
        {
            axis = new double[3];
            button = new bool[6];
        }

        public bool Init()
        {
            // Initialize DirectInput
            var directInput = new DirectInput();

            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                //log.MsgWriteLine(LogWindow.MsgTypes.Error, "No joystick/Gamepad found.");
                return false;
            }

            // Instantiate the joystick
            dxJoystick = new SharpDX.DirectInput.Joystick(directInput, joystickGuid);

            //log.MsgWriteLine(LogWindow.MsgTypes.Normal, "Found Joystick/Gamepad with GUID: {0}", joystickGuid);

            // Query all suported ForceFeedback effects
            var allEffects = dxJoystick.GetEffects();
            //foreach (var effectInfo in allEffects)
                //log.MsgWriteLine(LogWindow.MsgTypes.Normal, "Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            dxJoystick.Properties.BufferSize = 128;

            // Acquire the joystick
            dxJoystick.Acquire();

            return true;
        }

        public void Update()
        {
            if (dxJoystick == null)
                return;

            // Poll events from joystick
            dxJoystick.Poll();
            var data = dxJoystick.GetBufferedData();
            foreach (var state in data)
            {
                switch (state.Offset)
                {
                    // left stick: [0, 65535] = [left max, right max]
                    case JoystickOffset.X:
                        axis[0] = Math.Min(Math.Max(-1.0, ((double)(state.Value - 32768)/(double)32768)), 1.0);
                        break;
                    // left stick: [0, 65535] = [up max, down max]
                    case JoystickOffset.Y:
                        axis[1] = Math.Min(Math.Max(-1.0, ((double)(state.Value - 32768) / (double)32768)), 1.0);
                        break;
                    // left throttle: [32768, 65535] = [neutral, full]
                    case JoystickOffset.Z:
                        axis[2] = Math.Min(Math.Max(0.0, ((double)(state.Value - 32768) / (double)32768)), 1.0);
                        break;
                    // A button: [zero, nonzero] = [release, push]
                    case JoystickOffset.Buttons0:
                        button[0] = (state.Value != 0);
                        break;
                    // B
                    case JoystickOffset.Buttons1:
                        button[1] = (state.Value != 0);
                        break;
                    // X
                    case JoystickOffset.Buttons2:
                        button[2] = (state.Value != 0);
                        break;
                    // Y
                    case JoystickOffset.Buttons3:
                        button[3] = (state.Value != 0);
                        break;
                    // L
                    case JoystickOffset.Buttons4:
                        button[4] = (state.Value != 0);
                        break;
                    // R
                    case JoystickOffset.Buttons5:
                        button[5] = (state.Value != 0);
                        break;

                        //case JoystickOffset.Sliders0:
                        //    param.MotorGainX = 1.0f - (float)state.Value / (2 << 16);
                        //    param.MotorGainX *= param.SliderGain;
                        //    param.MotorGainY = param.MotorGainX;
                        //    break;
                        //case JoystickOffset.PointOfViewControllers0:
                        //    switch (state.Value)
                        //    {
                        //        case -1:
                        //            poVCtrl0 = Direction.Stop;
                        //            break;
                        //        case 0:
                        //            poVCtrl0 = Direction.Up;
                        //            break;
                        //        case 9000:
                        //            poVCtrl0 = Direction.Right;
                        //            break;
                        //        case 18000:
                        //            poVCtrl0 = Direction.Down;
                        //            break;
                        //        case 27000:
                        //            poVCtrl0 = Direction.Left;
                        //            break;
                        //    }
                        //
                        //    if (poVCtrl0 == Direction.Stop)
                        //    {
                        //        outZ = 0;
                        //    }
                        //    break;
                        //case JoystickOffset.Buttons0:
                        //    stop = true;
                        //    break;
                        //case JoystickOffset.Buttons6:
                        //    MotorClient.Send("SHUTDOWN\n");
                        //    break;
                }

                //switch (poVCtrl0)
                //{
                //    case Direction.Up:
                //        outZ += 0.25;
                //        break;
                //    case Direction.Down:
                //        outZ -= 0.25;
                //        break;
                //}
            }
        }
    }
}
