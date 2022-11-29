using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui;

internal class MessageBox: Window
{
    internal enum DialogResult { None, Ok, Canceled, Closed, Aborted, Yes, No }
    [Flags]
    internal enum ButtonStyle { None=0, Ok=1, Cancel=2, OkCancel=3, Abort=4, OkAbort=5, YesNo=8 }

    internal DialogResult Result = DialogResult.None;
    private string _message = string.Empty;
    private Action<MessageBox>? _callback = null;
    private ButtonStyle _buttonStyle = ButtonStyle.None;
    
    internal MessageBox(
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
        this.Flags = flags;
        this._buttonStyle = buttonStyle;

        if ( size is not null )
            this.Size = size;
        else
        {
            // Size of the message
            Vector2 vSize = ImGui.CalcTextSize(message);
            vSize.Y += ImGui.GetStyle().FramePadding.Y;

            vSize += ImGui.GetStyle().WindowPadding * 2;

            // Add the height of the title.
            vSize.Y += ImGui.CalcTextSize( title ).Y;
            vSize.Y += ImGui.GetStyle().FramePadding.Y;

            // Add the height of the bottom buttons
            vSize.Y += Global.BUTTON_Y_SCALED;
            vSize.Y += ImGui.GetStyle().FramePadding.Y;

            this.Size = vSize;
        }
    }

    public override void Draw()
    {
        ImGui.TextWrapped( this._message );
        int btn_count = this._buttonStyle == ButtonStyle.YesNo || this._buttonStyle == ButtonStyle.OkCancel ? 2 : 1;
        float btn_width = ( (ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X * 2)/2 ) - btn_count - ImGui.GetStyle().ItemSpacing.Y;

        if ( (this._buttonStyle & ButtonStyle.Ok) == ButtonStyle.Ok )
        {
            if ( ImGui.Button( $"Ok##MessageBoxButton", new( btn_width, Global.BUTTON_Y_SCALED ) ) )
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
            if ( ImGui.Button( $"Cancel##MessageBoxButton", new( btn_width, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.Canceled;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }

            if ( this._buttonStyle != ButtonStyle.Abort )
                ImGui.SameLine();
        }

        if ( (this._buttonStyle & ButtonStyle.Abort) == ButtonStyle.Abort )
        {
            if ( ImGui.Button( $"Abort##MessageBoxButton", new( btn_width, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.Aborted;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }
        }

        if ( this._buttonStyle == ButtonStyle.YesNo )
        {
            if ( ImGui.Button( $"Yes##MessageBoxButton", new( btn_width, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.Yes;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }

            ImGui.SameLine();

            if ( ImGui.Button( $"No##MessageBoxButton", new( btn_width, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.No;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }
        }
    }
}
