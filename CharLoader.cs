using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using ReLogic.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using Terraria.ID;
using System.Text;

namespace CharLoader {
	public class CharLoader : Mod {
		public static CharLoader Instance => ModContent.GetInstance<CharLoader>();
		Dictionary<DynamicSpriteFont, int> charCount;
		public int CharCount(DynamicSpriteFont font) => charCount.TryGetValue(font, out int value) ? value : 0;
		Dictionary<DynamicSpriteFont, Dictionary<string, char>> namedChars;
		public override void Load() {
			charCount = [];
			namedChars = [];
		}
		public override void Unload() {
			charCount = null;
			namedChars = null;
		}
		static char GetNextIndex(DynamicSpriteFont font) {
			Dictionary<DynamicSpriteFont, int> charCount = Instance.charCount;
			charCount.TryAdd(font, 0);
			return (char)('\uE000' + charCount[font]++);
		}
		public static Dictionary<string, char> GetNamedCharSet(DynamicSpriteFont font) {
			if (!Instance.namedChars.TryGetValue(font, out Dictionary<string, char> namedChars)) Instance.namedChars[font] = namedChars = [];
			return namedChars;
		}
		public static char? GetNamedChar(DynamicSpriteFont font, string name) {
			Dictionary<string, char> namedChars = GetNamedCharSet(font);
			return namedChars.TryGetValue(name, out char index) ? (char?)index : (char?)null;
		}
		public override object Call(params object[] args) {
			switch (args[0]) {
				case "AddCharacter":
				case "AddChar": {
					return AddCharacter((DynamicSpriteFont)args[1], (Texture2D)args[2], (Rectangle)args[3], (Rectangle)args[4], (Vector3)args[5], args.Length > 6 ? (string)args[6] : null);
				}

				case "SetCharacter":
				case "SetChar": {
					((DynamicSpriteFont)args[1]).SpriteCharacters[(char)args[2]] = new((Texture2D)args[3], (Rectangle)args[4], (Rectangle)args[5], (Vector3)args[6]);
					return null;
				}

				case "GetCharacter":
				case "GetChar": {
					return GetNamedChar((DynamicSpriteFont)args[1], (string)args[2]);
				}
			}
			throw new ArgumentException($"{args[0]} is not a valid function, you probably have the wrong mod, this is {DisplayName} and all of its functions are listed in its description");
		}
		public static char AddCharacter(DynamicSpriteFont font, Texture2D texture, Rectangle glyph, Rectangle padding, Vector3 kerning, string name = null) {
			char index = GetNextIndex(font);
			font.SpriteCharacters[index] = new(texture, glyph, padding, kerning);
			if (name is not null) GetNamedCharSet(font)[name] = index;
			return index;
		}
	}
	public class ShowCharCommand : ModCommand {
		public override string Command => "showchar";
		public override CommandType Type => CommandType.Chat;
		public override void Action(CommandCaller caller, string input, string[] args) {
			StringBuilder characters = new();
			string error = null;
			for (int i = 0; i < args.Length; i++) {
				if (CharLoader.GetNamedChar(FontAssets.MouseText.Value, args[i]) is char _char) {
					characters.Append(_char);
				} else if ((args[i].StartsWith("0x") && TryParseHex(args[i][2..], out int index)) || int.TryParse(args[i], out index)) {
					if (index < 0) error = "Negative indices are not allowed";
					else if (index > 0x18FF) error = "The PUA this mod uses is U+E000–U+F8FF, so a maximum index of 18FF (6399) is allowed";
					characters.Append((char)('\uE000' + index));
				} else if (string.IsNullOrWhiteSpace(args[i])) {
					characters.Append(' ');
				} else {
					error = $"no character found with name {args[i]}";
				}
			}
			if (error is not null) {
				caller.Reply(error, Color.Red);
			} else {
				ChatMessage message = ChatManager.Commands.CreateOutgoingMessage(characters.ToString());
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					ChatHelper.SendChatMessageFromClient(message);
				} else if (Main.netMode == NetmodeID.SinglePlayer) {
					ChatManager.Commands.ProcessIncomingMessage(message, Main.myPlayer);
				}
			}
		}
		static bool TryParseHex(string input, out int value) {
			try {
				value = Convert.ToInt32(input, 16);
				return true;
			} catch (Exception) {
				value = default;
				return false;
			}
		}
	}
	public class CharCommand : ShowCharCommand {
		public override string Command => "char";
	}
	public class CharsCommand : ShowCharCommand {
		public override string Command => "chars";
	}
}