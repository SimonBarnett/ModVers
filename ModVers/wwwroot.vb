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
                                        Console.WriteLine("Priority web found in [{0}].", virtual.PhysicalPath)
                                        Dim dr As New DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.System))
                                        Return Replace(virtual.PhysicalPath, "%SystemDrive%\", dr.Root.FullName)

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
