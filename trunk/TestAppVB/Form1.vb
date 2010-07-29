Public Class Form1

    Public Sub New()
        InitializeComponent()

        ' only load files, etc. when NOT closing to install an update
        If Not automaticUpdater.ClosingForInstall Then
            ' load important files, etc.
            ' LoadFilesEtc();
        End If
    End Sub

    Private Sub automaticUpdater_ClosingAborted(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles automaticUpdater.ClosingAborted
        ' your app was preparing to close
        ' however the update wasn't ready so your app is going to show itself
        ' LoadFilesEtc();
    End Sub

    Private Sub automaticUpdater_ReadyToBeInstalled(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles automaticUpdater.ReadyToBeInstalled
        'automaticUpdater.InstallNow()
    End Sub
End Class

