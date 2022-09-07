using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui
{
    internal class MessageBox: Window
    {
        internal enum DialogResult { None, Ok, Canceled, Closed, Aborted, Yes, No }
        [Flags]
        internal enum ButtonStyle { None=0, Ok=1, Cancel=2, OkCancel=3, Abort=4, OkAbort=5, YesNo=8 }

        internal DialogResult Result = DialogResult.None;
        protected string _message = string.Empty;
        protected Action<MessageBox>? _callback = null;
        protected ButtonStyle _buttonStyle = ButtonStyle.None;
        
        public MessageBox(
            string title,
            string message,
            Action<MessageBox>? callback = null,
            Vector2? size = null,
            ImGuiWindowFlags flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse,
            ButtonStyle buttonStyle = ButtonStyle.OkCancel
            ) : base( title )
        {
            this.IsOpen = true;
            this._message = message;
            this._callback = callback;
            this.Size = size == null ? ImGuiHelpers.ScaledVector2(300, 300) : size;
            this.Flags = flags;
            this._buttonStyle = buttonStyle;
        }

        public override void Draw()
        {
            ImGui.TextWrapped( this._message );
            int btn_count = this._buttonStyle == ButtonStyle.Ok ? 1 : 2;
            float btn_width = (ImGui.GetWindowWidth()/2) - ((10* ImGuiHelpers.GlobalScale) * btn_count);

            if ( (this._buttonStyle & ButtonStyle.Ok) == ButtonStyle.Ok )
            {
                if ( ImGui.Button( $"Ok##MessageBoxButton", new( btn_width, 25 * ImGuiHelpers.GlobalScale ) ) )
                {
                    this.Result = DialogResult.Ok;
                    if ( this._callback != null ) this._callback( this );
                    WordsmithUI.RemoveWindow( this );
                }
                if ( this._buttonStyle != ButtonStyle.Ok )
                    ImGui.SameLine();
            }

            if ( (this._buttonStyle & ButtonStyle.Cancel) == ButtonStyle.Cancel )
            {
                if ( ImGui.Button( $"Cancel##MessageBoxButton", new( btn_width, 25 * ImGuiHelpers.GlobalScale ) ) )
                {
                    this.Result = DialogResult.Canceled;
                    if ( this._callback != null )
                        this._callback( this );
                    WordsmithUI.RemoveWindow( this );
                }

                if ( this._buttonStyle != ButtonStyle.Abort )
                    ImGui.SameLine();
            }

            else if ( (this._buttonStyle & ButtonStyle.Abort) == ButtonStyle.Abort )
            {
                if ( ImGui.Button( $"Abort##MessageBoxButton", new( btn_width, 25 * ImGuiHelpers.GlobalScale ) ) )
                {
                    this.Result = DialogResult.Aborted;
                    if ( this._callback != null )
                        this._callback( this );
                    WordsmithUI.RemoveWindow( this );
                }
            }

            if ( this._buttonStyle == ButtonStyle.YesNo )
            {
                if ( ImGui.Button( $"Yes##MessageBoxButton", new( btn_width, 25 * ImGuiHelpers.GlobalScale ) ) )
                {
                    this.Result = DialogResult.Yes;
                    if ( this._callback != null )
                        this._callback( this );
                    WordsmithUI.RemoveWindow( this );
                }

                ImGui.SameLine();

                if ( ImGui.Button( $"No##MessageBoxButton", new( btn_width, 25 * ImGuiHelpers.GlobalScale ) ) )
                {
                    this.Result = DialogResult.No;
                    if ( this._callback != null )
                        this._callback( this );
                    WordsmithUI.RemoveWindow( this );
                }
            }
        }
    }
}
