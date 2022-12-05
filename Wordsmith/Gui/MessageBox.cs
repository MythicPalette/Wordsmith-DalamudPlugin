using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Wordsmith.Gui;

internal class MessageBox: Window
{
    [DllImport( "user32.dll" )]
    [return: MarshalAs( UnmanagedType.Bool )]
    static extern bool GetWindowRect( HandleRef hWnd, out Rect lpRect );

    internal enum DialogResult { None, Ok, Canceled, Closed, Aborted, Yes, No }
    [Flags]
    internal enum ButtonStyle { None=0, Ok=1, Cancel=2, OkCancel=3, Abort=4, OkAbort=5, Yes=8, No=16, YesNo=24 }

    internal DialogResult Result = DialogResult.None;
    private string _message = string.Empty;
    private Action<MessageBox>? _callback = null;
    private ButtonStyle _buttonStyle = ButtonStyle.None;

    internal MessageBox(
        string title,
        string message,
        ButtonStyle buttonStyle = ButtonStyle.OkCancel,
        Action<MessageBox>? callback = null
        ) : base( title )
    {
        this.IsOpen = true;
        this._message = message;
        this._callback = callback;
        this.Flags |= ImGuiWindowFlags.NoMove;
        this.Flags |= ImGuiWindowFlags.NoScrollbar;
        this.Flags |= ImGuiWindowFlags.NoScrollWithMouse;
        this.Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        this.Flags |= ImGuiWindowFlags.NoCollapse;

        this._buttonStyle = buttonStyle;
        this.SizeCondition = ImGuiCond.Appearing;
        this.Size = ImGui.CalcTextSize(message);
    }

    /// <summary>
    /// Centers the GUI window to the game window.
    /// </summary>
    internal void Center()
    {
        // Get the pointer to the window handle.
        IntPtr hWnd = IntPtr.Zero;
        foreach ( Process pList in Process.GetProcesses() )
            if ( pList.ProcessName == "ffxiv_dx11" || pList.ProcessName == "ffxiv" )
                hWnd = pList.MainWindowHandle;

        // If failing to get the handle then abort.
        if ( hWnd == IntPtr.Zero )
            return;

        // Get the game window rectangle
        Rect rGameWindow;
        GetWindowRect( new( null, hWnd ), out rGameWindow );

        // Get the size of the current window.
        Vector2 vThisSize = ImGui.GetWindowSize();

        // Set the position.
        this.Position = rGameWindow.Position + new Vector2( rGameWindow.Size.X / 2 - vThisSize.X / 2, rGameWindow.Size.Y / 2 - vThisSize.Y / 2 );
    }

    public override void Draw()
    {
        ImGui.Text( this._message );
        int btn_count = 0;// this._buttonStyle == ButtonStyle.YesNo || this._buttonStyle == ButtonStyle.OkCancel ? 2 : 1;
        if ( (this._buttonStyle & ButtonStyle.Ok) != 0 )        btn_count++;
        if ( (this._buttonStyle & ButtonStyle.Cancel) != 0 )    btn_count++;
        if ( (this._buttonStyle & ButtonStyle.Abort) != 0 )     btn_count++;
        if ( (this._buttonStyle & ButtonStyle.Yes) != 0 )       btn_count++;
        if ( (this._buttonStyle & ButtonStyle.No) != 0 )        btn_count++;

        float fAvailableWidth = ImGui.GetWindowWidth();
        fAvailableWidth -= (ImGui.GetStyle().WindowPadding.X * 2);

        // For button width, Get the available width and subtract the spacing then divide by button
        // count.
        float fButtonSpacing = btn_count * ImGui.GetStyle().ItemSpacing.Y * 2;

        float fButtonWidth = (fAvailableWidth - fButtonSpacing) / 2;

        if ( (this._buttonStyle & ButtonStyle.Ok) == ButtonStyle.Ok )
        {
            if ( ImGui.Button( $"Ok##MessageBoxButton", new( fButtonWidth, Global.BUTTON_Y_SCALED ) ) )
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
            if ( ImGui.Button( $"Cancel##MessageBoxButton", new( fButtonWidth, Global.BUTTON_Y_SCALED ) ) )
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
            if ( ImGui.Button( $"Abort##MessageBoxButton", new( fButtonWidth, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.Aborted;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }
        }

        if ( this._buttonStyle == ButtonStyle.YesNo )
        {
            if ( ImGui.Button( $"Yes##MessageBoxButton", new( fButtonWidth, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.Yes;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }

            ImGui.SameLine();

            if ( ImGui.Button( $"No##MessageBoxButton", new( fButtonWidth, Global.BUTTON_Y_SCALED ) ) )
            {
                this.Result = DialogResult.No;
                if ( this._callback != null )
                    this._callback( this );
                WordsmithUI.RemoveWindow( this );
            }
        }
        Center();
    }

    public override void OnClose()
    {
        base.OnClose();
        this.Result = DialogResult.Aborted;
        if ( this._callback != null )
            this._callback( this );
        WordsmithUI.RemoveWindow( this );
    }
}
