Imports System.IO
Imports Microsoft.Web.Administration

Module wwwroot

    Public Function rootPath() As String

        Console.Write("Locating Web server...")
        Using sm As New ServerManager
            For Each site As Site In sm.Sites
                With site
                    For Each binding As Microsoft.Web.Administration.Binding In .Bindings
                        If binding.EndPoint.Port = 443 Then
                            For Each app As Microsoft.Web.Administration.Application In .Applications
                                For Each virtual As VirtualDirectory In app.VirtualDirectories
                                    If virtual.Path = "/" Then
                                        Console.WriteLine("Ok.")

                                        Dim dr As New DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.System))
                                        Dim ret As String = Replace(virtual.PhysicalPath, "%SystemDrive%\", dr.Root.FullName)
                                        Console.WriteLine("Priority web found in [{0}].", ret)

                                        Return RET

                                    End If
                                Next
                            Next
                        End If
                    Next

                End With

            Next
        End Using

        Console.WriteLine("Not found.")
        Throw New Exception("No local Priority website.")

    End Function

End Module
