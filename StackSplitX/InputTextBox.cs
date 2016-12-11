using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StackSplitX
{
    internal sealed class Caret
    {
        // TODO: add draw calls/blink timer (or do that in a different class)
        /// <summary>
        /// Current index of the caret.
        /// </summary>
        public int Index { get; private set; } = 0;

        /// <summary>
        /// Optional maximum index of the caret. 0 = no limit.
        /// </summary>
        private int MaxLength = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="maxLength">The optional max length of the string the caret is navigating.</param>
        public Caret(int maxLength)
        {
            // The caret must always be 1 index ahead so it must be able to be positioned thusly
            this.MaxLength = maxLength > 0 ? maxLength + 1 : maxLength;
        }

        /// <summary>
        /// Moves the caret forward.
        /// </summary>
        /// <param name="amount">The amount to advance by.</param>
        public void Advance(int amount, int textLength)
        {
            UpdateIndex(this.Index + amount, textLength);
        }

        /// <summary>
        /// Moves the caret back.
        /// </summary>
        /// <param name="amount">The amount to move back by.</param>
        public void Regress(int amount)
        {
            UpdateIndex(this.Index - amount);
        }
        
        /// <summary>
        /// Move the caret to the start.
        /// </summary>
        public void Start()
        {
            this.Index = 0;
        }

        /// <summary>
        /// Move the caret to the end of the text.
        /// </summary>
        /// <param name="textLength">Current length of the text.</param>
        public void End(int textLength)
        {
            this.Index = (this.MaxLength > 0) ? Math.Min(this.MaxLength, textLength) : textLength;
        }

        /// <summary>
        /// Moves the caret to the specified index if it's within bounds.
        /// </summary>
        /// <param name="newIndex">The new caret index.</param>
        private void UpdateIndex(int newIndex, int textLength = 0)
        {
            if (newIndex >= 0 && 
                (this.MaxLength == 0 || newIndex < this.MaxLength) && 
                (textLength == 0 || newIndex <= textLength))
            {
                this.Index = newIndex;
            }
        }
    }

    public class InputTextBox : IKeyboardSubscriber
    {
        // TODO: create proper event args
        public delegate void InputTextboxEvent(InputTextBox textbox);
        public event InputTextboxEvent OnClick;
        public event InputTextboxEvent OnSubmit;
        public event InputTextboxEvent OnGainFocus;
        public event InputTextboxEvent OnLoseFocus;

        public Vector2 Position { get; set; }
        public Vector2 Extent { get; set; }

        public Color TextColor { get; set; } = Game1.textColor;
        public SpriteFont Font { get; set; } = Game1.smallFont;
        public bool Selected { get; set; }
        public bool NumbersOnly { get; set; } = false;
        public string Text { get; private set; }

        //private Texture2D TextboxTexture;
        private int CharacterLimit = 0;
        private Caret Caret;

        public InputTextBox(int characterLimit = 0, string defaultText = "")
        {
            this.CharacterLimit = characterLimit;
            this.Caret = new Caret(characterLimit);

            AppendString(defaultText);
        }

        /* Begin IKeyboardSubscriber implementation */
        #region IKeyboardSubscriber implementation
        public void RecieveTextInput(char inputChar)
        {
            AppendCharacter(inputChar);
        }

        public void RecieveTextInput(string text)
        {
            AppendString(text);
        }

        public void RecieveCommandInput(char command)
        {
            // Cast the ascii value to the readable enum value
            Keys key = (Keys)command;
            switch (key)
            {
                case Keys.Back:
                    // TODO: handle deleting all highlighted characters
                    RemoveCharacterAtCaret();
                    break;
                case Keys.Enter:
                    Submit();
                    break;
                case Keys.Tab:
                    break;
            }
        }

        public void RecieveSpecialInput(Keys key)
        {
            switch (key)
            {
                case Keys.Left:
                    this.Caret.Regress(1);
                    break;
                case Keys.Right:
                    this.Caret.Advance(1, this.Text.Length);
                    break;
                case Keys.Home:
                    this.Caret.Start();
                    break;
                case Keys.End:
                    this.Caret.End(this.Text.Length);
                    break;
                // TODO: ctrl+A to select all
                // TODO: handle delete key which removes character on right side of caret
            }
        }
        #endregion IKeyboardSubscriber implementation
        /* End IKeyboardSubscriber implementation */

        private void Submit()
        {
            this.OnSubmit(this);
        }

        public void Update()
        {
            // TODO: handle highlighting and stuff
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Part of the spritesheet containing the texture we want to draw
            var menuTextureSourceRect = new Rectangle(0, 256, 60, 60);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, menuTextureSourceRect, (int)this.Position.X, (int)this.Position.Y, (int)this.Extent.X, (int)this.Extent.Y, Color.White);
            spriteBatch.DrawString(this.Font, this.Text, this.Position + new Vector2(Game1.tileSize / 4, Game1.tileSize / 3), Game1.textColor);

            var textDimensions = this.Font.MeasureString(this.Text.Length > 0 ? this.Text : " ");
            var letterWidth = (textDimensions.X / (this.Text.Length > 0 ? this.Text.Length : 1));
            int caretX = ((int)letterWidth * this.Caret.Index);
            // Offset by a small amount when were not at the end so the caret doesn't go on top of the letter
            caretX = (this.Caret.Index < this.Text.Length) ? caretX - Game1.pixelZoom : caretX;
            spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)this.Position.X + Game1.tileSize / 4 + caretX + Game1.pixelZoom, (int)this.Position.Y + Game1.tileSize / 3 - Game1.pixelZoom, 4, (int)textDimensions.Y), this.TextColor);
        }

        #region Text manipulation
        private void AppendString(string s)
        {
            if (s == null || s.Length == 0)
                return;

            int dummy = 0;
            if (this.NumbersOnly && !int.TryParse(s, out dummy))
                return;

            if (this.CharacterLimit > 0 && s.Length > this.CharacterLimit)
            {
                this.Text = s.Remove(this.CharacterLimit - 1);
            }
            else
            {
                this.Text = s;
            }

            // Move the caret to the end
            this.Caret.End(this.Text.Length);
        }

        private void AppendCharacter(char c)
        {
            if ((this.CharacterLimit == 0 || this.Text.Length < this.CharacterLimit) &&
                (!this.NumbersOnly || char.IsDigit(c)))
            {
                this.Text += c;
                this.Caret.Advance(1, this.Text.Length);
            }
        }

        private void RemoveCharacterAtCaret()
        {
            // The caret is always to the right of the character we want to remove.
            if (this.Caret.Index > 0)
            {
                this.Text = this.Text.Remove(this.Caret.Index - 1, 1);
                this.Caret.Regress(1);
            }
        }
        #endregion Text manipulation
    }
}
