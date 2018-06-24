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
        public int    zeroPosX = -1;
        public int    zeroPosY = -1;
        public int    zeroPosZ = -1;
        public double outX = 0;
        public double outY = 0;
        public double outZ = 0;
        public Direction poVCtrl0 = Direction.Stop;

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
                    case JoystickOffset.X:
                        if (zeroPosX < 0) zeroPosX = state.Value;
                        outX = (double)(state.Value - zeroPosX) / (2 << 14);
                        break;
                    case JoystickOffset.Y:
                        if (zeroPosY < 0) zeroPosY = state.Value;
                        outY = (double)(state.Value - zeroPosY) / (2 << 14);
                        outY = -outY;
                        break;
                    //case JoystickOffset.Sliders0:
                    //    param.MotorGainX = 1.0f - (float)state.Value / (2 << 16);
                    //    param.MotorGainX *= param.SliderGain;
                    //    param.MotorGainY = param.MotorGainX;
                    //    break;
                    case JoystickOffset.PointOfViewControllers0:
                        switch (state.Value)
                        {
                            case -1:
                                poVCtrl0 = Direction.Stop;
                                break;
                            case 0:
                                poVCtrl0 = Direction.Up;
                                break;
                            case 9000:
                                poVCtrl0 = Direction.Right;
                                break;
                            case 18000:
                                poVCtrl0 = Direction.Down;
                                break;
                            case 27000:
                                poVCtrl0 = Direction.Left;
                                break;
                        }

                        if (poVCtrl0 == Direction.Stop)
                        {
                            outZ = 0;
                        }
                        break;
                    //case JoystickOffset.Buttons0:
                    //    stop = true;
                    //    break;
                    //case JoystickOffset.Buttons6:
                    //    MotorClient.Send("SHUTDOWN\n");
                    //    break;
                }

                switch (poVCtrl0)
                {
                    case Direction.Up:
                        outZ += 0.25;
                        break;
                    case Direction.Down:
                        outZ -= 0.25;
                        break;
                }
            }
        }
    }
}
