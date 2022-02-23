using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Contains a command entered in the console or in-game.
	/// </summary>
	public class ConsoleCmd : DemoPacket {

		public string Command;
		private static readonly Regex KeyPressRegex = new Regex(@"^\s*[+-]\w+\s+(\d{1,3})\s*$", RegexOptions.Compiled);
		public ButtonCode? ButtonCode;

		public static implicit operator string(ConsoleCmd cmd) => cmd.Command;


		public ConsoleCmd(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Command = bsr.ReadStringOfLength(bsr.ReadSInt());
			TimingAdjustment.AdjustFromConsoleCmd(this);
			Match match = KeyPressRegex.Match(Command);
			if (match.Success) {
				int val = int.Parse(match.Groups[1].Value);
				if (val >= 0 && val < (int)Packets.ButtonCode.LAST)
					ButtonCode = (ButtonCode)val;
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(Command);
			if (ButtonCode.HasValue)
				pw.Append($"   ({ButtonCode.Value})");
		}
	}


	// incorrect if using a steam controller
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[SuppressMessage("ReSharper", "IdentifierTypo")]
	public enum ButtonCode {
		BUTTON_CODE_INVALID = -1,
		KEY_NONE,
		KEY_0,
		KEY_1,
		KEY_2,
		KEY_3,
		KEY_4,
		KEY_5,
		KEY_6,
		KEY_7,
		KEY_8,
		KEY_9,
		KEY_A,
		KEY_B,
		KEY_C,
		KEY_D,
		KEY_E,
		KEY_F,
		KEY_G,
		KEY_H,
		KEY_I,
		KEY_J,
		KEY_K,
		KEY_L,
		KEY_M,
		KEY_N,
		KEY_O,
		KEY_P,
		KEY_Q,
		KEY_R,
		KEY_S,
		KEY_T,
		KEY_U,
		KEY_V,
		KEY_W,
		KEY_X,
		KEY_Y,
		KEY_Z,
		KEY_PAD_0,
		KEY_PAD_1,
		KEY_PAD_2,
		KEY_PAD_3,
		KEY_PAD_4,
		KEY_PAD_5,
		KEY_PAD_6,
		KEY_PAD_7,
		KEY_PAD_8,
		KEY_PAD_9,
		KEY_PAD_DIVIDE,
		KEY_PAD_MULTIPLY,
		KEY_PAD_MINUS,
		KEY_PAD_PLUS,
		KEY_PAD_ENTER,
		KEY_PAD_DECIMAL,
		KEY_LBRACKET,
		KEY_RBRACKET,
		KEY_SEMICOLON,
		KEY_APOSTROPHE,
		KEY_BACKQUOTE,
		KEY_COMMA,
		KEY_PERIOD,
		KEY_SLASH,
		KEY_BACKSLASH,
		KEY_MINUS,
		KEY_EQUAL,
		KEY_ENTER,
		KEY_SPACE,
		KEY_BACKSPACE,
		KEY_TAB,
		KEY_CAPSLOCK,
		KEY_NUMLOCK,
		KEY_ESCAPE,
		KEY_SCROLLLOCK,
		KEY_INSERT,
		KEY_DELETE,
		KEY_HOME,
		KEY_END,
		KEY_PAGEUP,
		KEY_PAGEDOWN,
		KEY_BREAK,
		KEY_LSHIFT,
		KEY_RSHIFT,
		KEY_LALT,
		KEY_RALT,
		KEY_LCONTROL,
		KEY_RCONTROL,
		KEY_LWIN,
		KEY_RWIN,
		KEY_APP,
		KEY_UP,
		KEY_LEFT,
		KEY_DOWN,
		KEY_RIGHT,
		KEY_F1,
		KEY_F2,
		KEY_F3,
		KEY_F4,
		KEY_F5,
		KEY_F6,
		KEY_F7,
		KEY_F8,
		KEY_F9,
		KEY_F10,
		KEY_F11,
		KEY_F12,
		KEY_CAPSLOCKTOGGLE,
		KEY_NUMLOCKTOGGLE,
		KEY_SCROLLLOCKTOGGLE,

		MOUSE_LEFT,
		MOUSE_RIGHT,
		MOUSE_MIDDLE,
		MOUSE_4,
		MOUSE_5,
		MOUSE_WHEEL_UP,   // A fake button which is 'pressed' and 'released' when the wheel is moved up
		MOUSE_WHEEL_DOWN, // A fake button which is 'pressed' and 'released' when the wheel is moved down

		// xbox

		KEY_XBUTTON_A,
		KEY_XBUTTON_B,
		KEY_XBUTTON_X,
		KEY_XBUTTON_Y,
		KEY_XBUTTON_LEFT_SHOULDER,
		KEY_XBUTTON_RIGHT_SHOULDER,
		KEY_XBUTTON_BACK,
		KEY_XBUTTON_START,
		KEY_XBUTTON_STICK1,
		KEY_XBUTTON_STICK2,

		KEY_XBUTTON_UP = KEY_XBUTTON_A + ButtonCount.JOYSTICK_MAX_BUTTON_COUNT,
		KEY_XBUTTON_RIGHT,
		KEY_XBUTTON_DOWN,
		KEY_XBUTTON_LEFT,

		KEY_XSTICK1_RIGHT = KEY_XBUTTON_UP + ButtonCount.JOYSTICK_POV_BUTTON_COUNT, // XAXIS POSITIVE
		KEY_XSTICK1_LEFT,     // XAXIS NEGATIVE
		KEY_XSTICK1_DOWN,     // YAXIS POSITIVE
		KEY_XSTICK1_UP,       // YAXIS NEGATIVE
		KEY_XBUTTON_LTRIGGER, // ZAXIS POSITIVE
		KEY_XBUTTON_RTRIGGER, // ZAXIS NEGATIVE
		KEY_XSTICK2_RIGHT,    // UAXIS POSITIVE
		KEY_XSTICK2_LEFT,     // UAXIS NEGATIVE
		KEY_XSTICK2_DOWN,     // VAXIS POSITIVE
		KEY_XSTICK2_UP,       // VAXIS NEGATIVE

		NOVINT_LOGO_0 = KEY_XSTICK1_RIGHT + ButtonCount.JOYSTICK_AXIS_BUTTON_COUNT + 1,
		NOVINT_TRIANGLE_0,
		NOVINT_BOLT_0,
		NOVINT_PLUS_0,
		NOVINT_LOGO_1,
		NOVINT_TRIANGLE_1,
		NOVINT_BOLT_1,
		NOVINT_PLUS_1,

		STEAMCONTROLLER_A,
		STEAMCONTROLLER_B,
		STEAMCONTROLLER_X,
		STEAMCONTROLLER_Y,
		STEAMCONTROLLER_DPAD_UP,
		STEAMCONTROLLER_DPAD_RIGHT,
		STEAMCONTROLLER_DPAD_DOWN,
		STEAMCONTROLLER_DPAD_LEFT,
		STEAMCONTROLLER_LEFT_BUMPER,
		STEAMCONTROLLER_RIGHT_BUMPER,
		STEAMCONTROLLER_LEFT_TRIGGER,
		STEAMCONTROLLER_RIGHT_TRIGGER,
		STEAMCONTROLLER_LEFT_GRIP,
		STEAMCONTROLLER_RIGHT_GRIP,
		STEAMCONTROLLER_LEFT_PAD_FINGERDOWN,
		STEAMCONTROLLER_RIGHT_PAD_FINGERDOWN,
		STEAMCONTROLLER_LEFT_PAD_CLICK,
		STEAMCONTROLLER_RIGHT_PAD_CLICK,
		STEAMCONTROLLER_LEFT_PAD_UP,
		STEAMCONTROLLER_LEFT_PAD_RIGHT,
		STEAMCONTROLLER_LEFT_PAD_DOWN,
		STEAMCONTROLLER_LEFT_PAD_LEFT,
		STEAMCONTROLLER_RIGHT_PAD_UP,
		STEAMCONTROLLER_RIGHT_PAD_RIGHT,
		STEAMCONTROLLER_RIGHT_PAD_DOWN,
		STEAMCONTROLLER_RIGHT_PAD_LEFT,
		STEAMCONTROLLER_SELECT,
		STEAMCONTROLLER_START,
		STEAMCONTROLLER_STEAM,
		STEAMCONTROLLER_INACTIVE_START,
		STEAMCONTROLLER_F1,
		STEAMCONTROLLER_F2,
		STEAMCONTROLLER_F3,
		STEAMCONTROLLER_F4,
		STEAMCONTROLLER_F5,
		STEAMCONTROLLER_F6,
		STEAMCONTROLLER_F7,
		STEAMCONTROLLER_F8,
		STEAMCONTROLLER_F9,
		STEAMCONTROLLER_F10,
		STEAMCONTROLLER_F11,
		STEAMCONTROLLER_F12,

		LAST
	}


	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal enum ButtonCount {
		JOYSTICK_MAX_BUTTON_COUNT = 32,
		JOYSTICK_POV_BUTTON_COUNT = 4,
		JOYSTICK_AXIS_BUTTON_COUNT = 12,
	}
}
