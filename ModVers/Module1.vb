﻿Imports System.Data.SqlClient
Imports System.IO
Imports System.Xml
Imports System.Xml.Serialization

Module Module1

    Private modverURL = "http://soti.medatechuk.com/client/modvers.xml"
    Private MVer As New Dictionary(Of String, Integer)

    Sub Main()

        Try

            ' Find the local Priority sql database
            Dim db As ServerInstance = SearchInstance()

            ' Find the path to the priority website
            Dim out As FileInfo = outfile()

            ' Load module data from offsite web server
            Dim mv As modver.modver = modData()
            Console.WriteLine("Ok.")

            ' Open the database
            Console.Write("Connecing to [{0}]...", db.ConnectionString)
            Using cn As New SqlConnection(db.ConnectionString)
                Console.WriteLine("Ok.")
                With mv
                    For Each m As modver.module In .moduleCollection

                        MVer.Add(m.name, 0)

                        Console.WriteLine("Verifying module [{0}].", m.name)
                        For Each v As modver.version In m.versionCollection
                            Console.WriteLine("Verifying version number [{0}].", v.number.ToString)

                            Dim pass As Boolean = True
                            For Each ch As modver.check In v.checkCollection
                                Select Case ch.type.ToLower
                                    Case "table"
                                        Console.WriteLine("Check TABLE [{0}].", ch.name)
                                        For Each col As modver.Column In ch.checkCollection
                                            Console.WriteLine("Check TABLE Column [{0}.{1}].", ch.name, col.name)

                                        Next

                                    Case "form"
                                        Console.WriteLine("Check FORM [{0}].", ch.name)
                                        For Each col As modver.Column In ch.checkCollection
                                            Console.WriteLine("Check FORM Column [{0}.{1}].", ch.name, col.name)

                                        Next

                                    Case "sql"
                                        Console.WriteLine("Check sql in [{0}] database.", ch.db)
                                        Select Case ch.db
                                            Case "system"
                                                Console.WriteLine("Execute sql: USE [{0}];{1}", ch.db, ch.name)

                                            Case "user"
                                                Console.WriteLine("Execute sql: USE [{0}];{1}", PriorityEnviroments(0), ch.name)

                                        End Select

                                End Select

                            Next ' Check

                            If pass Then
                                MVer(m.name) = v.number
                            Else
                                Exit For
                            End If

                        Next ' Version

                    Next 'Module

                End With

            End Using

            SaveResult(out)

        Catch ex As Exception
            Console.WriteLine(ex.Message)

        End Try

        Console.ReadKey()

    End Sub

#Region "Private Methods"
    Private Function SearchInstance() As ServerInstance

        Console.Write("Locating SQL server...")
        For Each s As ServerInstance In EnumerateServers()
            If AttemptConnect(s) Then
                Console.WriteLine("Ok.")
                CheckEnvironment(s)
                Console.WriteLine("Found instance [{0}].", s.ConnectionString)
                Return s
            End If
        Next

        Console.WriteLine("Not found.")
        Throw New Exception("No local Priority database.")

    End Function

    Private Function outfile() As FileInfo

        Return New FileInfo(IO.Path.Combine(rootPath, "modver.xml"))

    End Function

    Private Function modData() As modver.modver
        Console.Write("Load module info from {0}...", modverURL)
        Try
            Return New XmlSerializer(
                GetType(
                    modver.modver
                )
            ).Deserialize(
                XmlReader.Create(
                    modverURL
                )
            )

        Catch ex As Exception
            Console.WriteLine("Fail.")
            Throw New Exception(
                String.Format(
                    "Error opening {0}. {1}",
                    modverURL,
                    ex.Message
                )
            )

        End Try

    End Function

    Private Sub SaveResult(fn As FileInfo)

        ' Delete output file if exists
        If fn.Exists Then
            Console.Write("{0} exists. Deleting...", fn.FullName)
            Try
                fn.Delete()
                Console.WriteLine("Ok.")

            Catch ex As Exception
                Console.WriteLine("Fail.")
                Throw New Exception(
                    String.Format(
                        "Could not delete {0}.",
                        ex.Message
                    )
                )

            End Try

        End If

        Console.Write("Writing {0}...", fn.FullName)
        Using xr As XmlWriter = XmlWriter.Create(fn.FullName)
            xr.WriteStartDocument()
            xr.WriteStartElement("modver")
            For Each m As String In MVer.Keys
                xr.WriteStartElement("module")
                xr.WriteAttributeString("name", m.ToString)
                xr.WriteAttributeString("version", MVer(m).ToString)
                xr.WriteEndElement()
            Next
            xr.WriteEndElement()
            xr.WriteEndDocument()

        End Using
        Console.WriteLine("Ok.")

    End Sub

#End Region

End Module
