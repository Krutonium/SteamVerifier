Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Win32
Public Class Form1
    Private Declare Function SetForegroundWindow Lib "user32" _
    (ByVal hWnd As IntPtr) As Boolean
    Private Declare Sub keybd_event Lib "user32" (ByVal bVk As _
    Byte, ByVal bScan As Byte, ByVal dwFlags As Integer,
    ByVal dwExtraInfo As Integer)
    Private Const VK_SNAPSHOT As Short = &H2CS

    Public Overloads Declare Function GetWindowRect Lib _
    "User32" Alias "GetWindowRect" (ByVal hWnd As IntPtr,
    ByRef lpRect As RECT) As Int32
    <StructLayout(LayoutKind.Sequential)>
    Public Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure


    Dim Percent As String
    Dim CurrentGame As String

    Dim UserPath As String
    Dim IsValidating As Boolean = False
    Dim GameIDList As New List(Of String)
    Dim Done As New List(Of String)
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Icon = My.Resources.Steam
        PictureBox1.Image = My.Resources.NotRunning
        'HKEY_CURRENT_USER\\Software\\Valve\\Half-Life\\InstallPath
        Dim SteamPath As String = ""
        Try
            Dim Reg As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, Microsoft.Win32.RegistryView.Registry64)
            Reg = Reg.OpenSubKey("SOFTWARE\Valve\Steam")
            For Each v In Reg.GetValueNames
                If v = "SteamPath" Then
                    SteamPath = Reg.GetValue(v)      'Yes, I had to do it this way, because for some reason just asking for the value of SteamPath gave null.
                End If
            Next
        Catch ex As Exception

        End Try
        If SteamPath = "" Then
            Call ChooseNewDir()
            SteamPath = My.Settings.GameDirOther
        End If

        Dim Dirs As New List(Of String)
        Dirs.Add(SteamPath & "\steamapps")
        Try
            Dim vdf As New List(Of String)
            vdf = File.ReadAllLines(UserPath & "\steamapps\libraryfolders.vdf").ToList
            For Each line In vdf
                line = line.Trim
                If line.Contains(":\\") Then
                    Dim split = line.Split("""")
                    For Each l In split
                        If l.Contains(":\\") Then
                            l = l.Replace("\\", "\")
                            Dirs.Add(l & "\steamapps")
                        End If
                    Next
                End If
            Next
        Catch ex As Exception
        End Try
        For Each UserPath In Dirs
            For Each f In Directory.GetFiles(UserPath)
                If f.EndsWith(".acf") Then
                    'GameIDList.Add(Path.GetFileNameWithoutExtension(f))
                    Dim GTemp As String = Path.GetFileNameWithoutExtension(f)
                    GTemp = GTemp.Replace("appmanifest_", "")
                    If My.Settings.BlackList.Contains(GTemp) = False Then
                        GameIDList.Add(GTemp)
                    End If
                End If
            Next
        Next
    End Sub
    Private Sub ChooseNewDir()
        FolderBrowserDialog.ShowNewFolderButton = False
        FolderBrowserDialog.ShowDialog()
        'MsgBox(FolderBrowserDialog.SelectedPath)
        My.Settings.GameDirOther = FolderBrowserDialog.SelectedPath
        My.Settings.Save()
        UserPath = My.Settings.GameDirOther
    End Sub
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        CheckProgressAndSaveImage(CurrentGame)
        If IsValidating = False Then
            Timer1.Enabled = False
        End If
    End Sub

    Private Sub CheckProgressAndSaveImage(ByVal Game As String)
        For Each P In Process.GetProcesses
            If P.MainWindowTitle.Trim = "" = False Then
                If P.MainWindowTitle.ToUpper.Contains("VALIDATING") Then
                    Dim Title = P.MainWindowTitle.Split("-"c)
                    Dim Perc = Title(1).Split("%"c)
                    Percent = Perc(0).Trim
                    CheckingProgress.Value = Percent
                    If Percent = "100" Then
                        Dim bm As Bitmap = GetProgramImage(P.MainWindowHandle)
                        If Directory.Exists(My.Settings.ScreenShotDir) = False Then
                            Directory.CreateDirectory(My.Settings.ScreenShotDir)
                        End If
                        bm.Save(My.Settings.ScreenShotDir & Game & ".png", System.Drawing.Imaging.ImageFormat.Png)
                        P.CloseMainWindow()
                        Thread.Sleep(2000)
                        IsValidating = False
                    End If
                End If
            End If
        Next
    End Sub
    Private Function GetProgramImage(ByVal handle As IntPtr) As Image
        ' Bring it to the top.
        SetForegroundWindow(handle)

        ' Get the desktop image.
        Dim desktop_bm As Bitmap = GetDesktopImage()

        ' Find the program's window.
        Dim program_rect As RECT
        GetWindowRect(handle, program_rect)

        ' Copy that part of the desktop image.
        Dim wid As Integer = program_rect.Right -
            program_rect.Left
        Dim hgt As Integer = program_rect.Bottom -
            program_rect.Top
        Dim dest_rect As New Rectangle(0, 0, wid, hgt)
        Dim src_rect As New Rectangle(program_rect.Left,
            program_rect.Top, wid, hgt)
        Dim new_bm As New Bitmap(wid, hgt)
        Using gr As Graphics = Graphics.FromImage(new_bm)
            gr.DrawImage(desktop_bm, dest_rect, src_rect,
                GraphicsUnit.Pixel)
        End Using

        Return new_bm
    End Function
    Private Function GetDesktopImage() As Image
        keybd_event(System.Windows.Forms.Keys.Snapshot, 0, 0, 0)
        System.Threading.Thread.Sleep(200)

        If Clipboard.ContainsImage() Then Return _
            Clipboard.GetImage()
        Return Nothing
    End Function
    Private Sub cmdChangeSteamAppsFolder_Click(sender As Object, e As EventArgs)
        ChooseNewDir()
    End Sub
    Private Sub ChooseSteamAppsFolderToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ChooseSteamAppsFolderToolStripMenuItem.Click
        ChooseNewDir()
    End Sub

    Private Sub CloseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CloseToolStripMenuItem.Click
        Me.Close()
    End Sub

    Sub UpdateImage(ByVal GameID As Integer)
        Try
            Dim ImageURL As String = "http://cdn.akamai.steamstatic.com/steam/apps/" & GameID & "/header.jpg"
            Dim WS As New WebClient
            Dim byteImage As Byte() = WS.DownloadData(ImageURL) 'this should contain your data
            Dim myimage As Image
            Dim ms As System.IO.MemoryStream = New System.IO.MemoryStream(byteImage)
            myimage = System.Drawing.Image.FromStream(ms)
            PictureBox1.Image = myimage
        Catch ex As Exception
        End Try
    End Sub

    Private Sub DbToolStripMenuItem1_Click(sender As Object, e As EventArgs)
        For Each item In GameIDList
            UpdateImage(item)
            MsgBox("Waiting")
        Next
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        IsValidating = False
        checkIfValidDone.Enabled = True
        Button1.Enabled = False
    End Sub

    Private Sub checkIfValidDone_Tick(sender As Object, e As EventArgs) Handles checkIfValidDone.Tick
        If IsValidating = False Then
            For Each item In GameIDList
                If Done.Contains(item) = False Then
                    IsValidating = True
                    Process.Start("steam://validate/" & item)
                    CurrentGame = item
                    Done.Add(item)
                    UpdateImage(item)
                    Timer1.Enabled = True
                    Exit For
                End If
            Next
        End If
        If IsValidating = False Then
            Button1.Enabled = True
            checkIfValidDone.Enabled = False
            MsgBox("Verification of your games is complete!", MsgBoxStyle.OkOnly, "Verified!")
        End If
    End Sub

    Private Sub OpenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OpenToolStripMenuItem.Click
        Process.Start(My.Settings.ScreenShotDir)
    End Sub

    Private Sub ChangeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ChangeToolStripMenuItem.Click
        Dim J As New FolderBrowserDialog
        J.Description = "Please choose where you want the result shots to go:"
        J.ShowNewFolderButton = True
        J.ShowDialog()
        If J.SelectedPath = "" = False Then
            My.Settings.ScreenShotDir = J.SelectedPath
        End If
    End Sub

    Private Sub ResetToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ResetToolStripMenuItem.Click
        Dim P = MsgBox("Are you sure?", MsgBoxStyle.YesNo)
        If P = MsgBoxResult.Yes Then
            My.Settings.BlackList = Nothing
            My.Settings.BlackList.Add(" ")
        End If
    End Sub

    Private Sub ManageToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ManageToolStripMenuItem.Click

        My.Settings.BlackList.Add(InputBox("Please enter the Game ID to blacklist.", "Blacklist"))
        Dim TempList As New List(Of String)
        For Each item In GameIDList
            If My.Settings.BlackList.Contains(item) = False Then
                TempList.Add(item)
            End If
        Next
        GameIDList = TempList
        TempList = Nothing
    End Sub

    Private Sub PictureBox2_Click(sender As Object, e As EventArgs) Handles PictureBox2.Click
        Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=ELBKFBFBRBT46")
    End Sub
End Class
