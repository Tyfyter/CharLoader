using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using ReLogic.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CharLoader {
	public class CharLoader : Mod {
		public static CharLoader Instance => ModContent.GetInstance<CharLoader>();
		Dictionary<DynamicSpriteFont, int> charCount;
		public int CharCount(DynamicSpriteFont font) => charCount.TryGetValue(font, out int value) ? value : 0;
		Dictionary<DynamicSpriteFont, Dictionary<string, char>> namedChars;
		public override void Load() {
			charCount = new();
			namedChars = new();
			FieldInfo spriteCharacters = typeof(DynamicSpriteFont).GetField("_spriteCharacters", BindingFlags.NonPublic | BindingFlags.Instance);
			Type SpriteCharacterData = typeof(DynamicSpriteFont).GetNestedType("SpriteCharacterData", BindingFlags.NonPublic | BindingFlags.Instance);
			Type dict = typeof(Dictionary<,>).MakeGenericType(typeof(char), SpriteCharacterData);
			MethodInfo set = dict.GetMethod("set_Item");
			ConstructorInfo ctor = SpriteCharacterData.GetConstructor(new Type[] {
				typeof(Texture2D),
				typeof(Rectangle),
				typeof(Rectangle),
				typeof(Vector3)
			});
			DynamicMethod getterMethod = new DynamicMethod("AddCharacter", typeof(void), new Type[] {
				typeof(DynamicSpriteFont),
				typeof(char),
				typeof(Texture2D),
				typeof(Rectangle),
				typeof(Rectangle),
				typeof(Vector3)
			}, true);
			ILGenerator gen = getterMethod.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldfld, spriteCharacters);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Ldarg_2);
			gen.Emit(OpCodes.Ldarg_3);
			gen.Emit(OpCodes.Ldarg_S, (byte)4);
			gen.Emit(OpCodes.Ldarg_S, (byte)5);
			gen.Emit(OpCodes.Newobj, ctor);
			gen.Emit(OpCodes.Call, set);
			gen.Emit(OpCodes.Ret);

			_SetCharacter = getterMethod.CreateDelegate<SetCharFunc>();
		}
		public override void Unload() {
			charCount = null;
			namedChars = null;
		}
		static char GetNextIndex(DynamicSpriteFont font) {
			var charCount = Instance.charCount;
			if (!charCount.ContainsKey(font)) charCount.Add(font, 0);
			return (char)('\uE000' + charCount[font]++);
		}
		public static Dictionary<string, char> GetNamedCharSet(DynamicSpriteFont font) {
			if (!Instance.namedChars.TryGetValue(font, out var namedChars)) Instance.namedChars[font] = namedChars = new();
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
					SetCharacter((DynamicSpriteFont)args[1], (char)args[2], (Texture2D)args[3], (Rectangle)args[4], (Rectangle)args[5], (Vector3)args[6]);
					return null;
				}

				case "GetCharacter":
				case "GetChar": {
					return GetNamedChar((DynamicSpriteFont)args[1], (string)args[2]);
				}
			}
			throw new ArgumentException($"{args[0]} is not a valid function, you probably have the wrong mod, this is {DisplayName} and all of its functions are listed in its description");
		}
		public delegate void SetCharFunc(DynamicSpriteFont font, char index, Texture2D texture, Rectangle glyph, Rectangle padding, Vector3 kerning);
		SetCharFunc _SetCharacter;
		public static void SetCharacter(DynamicSpriteFont font, char index, Texture2D texture, Rectangle glyph, Rectangle padding, Vector3 kerning) {
			Instance._SetCharacter(font, index, texture, glyph, padding, kerning);
		}
		public static char AddCharacter(DynamicSpriteFont font, Texture2D texture, Rectangle glyph, Rectangle padding, Vector3 kerning, string name = null) {
			char index = GetNextIndex(font);
			SetCharacter(font, index, texture, glyph, padding, kerning);
			if (name is not null) GetNamedCharSet(font)[name] = index;
			return index;
		}
	}
	public class ShowCharCommand : ModCommand {
		public override string Command => "showchar";
		public override CommandType Type => CommandType.Chat;
		public override void Action(CommandCaller caller, string input, string[] args) {
			string character = null;
			string error = null;
			if (CharLoader.GetNamedChar(FontAssets.MouseText.Value, args[0]) is char _char) {
				character = _char.ToString();
			} else if (int.TryParse(args[0], out int index) || TryParseHex(args[0], out index)) {
				if (index < 0) error = "Negative indecies are not allowed";
				else if (index > 0x18FF) error = "The PUA I'm using is U+E000–U+F8FF, so a maximum index of 18FF (6399) is allowed";
				character = ((char)('\uE000' + index)).ToString();
			} else {
				error = $"no character found with name {args[0]}";
			}
			if (error is not null) {
				Main.NewText(error, Color.Red);
			} else {
				Main.NewText(character);
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
}