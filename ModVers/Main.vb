Imports System.Data.SqlClient
Imports System.IO
Imports System.Xml
Imports System.Xml.Serialization

Module Main

    Private arg As clArg
    Private modverURL = "https://priority.medatechuk.com/api/live"
    Private MVer As New Dictionary(Of String, Integer)

    Sub Main(ByVal args() As String)

        Try
            arg = New clArg(args)

            If arg.Keys.Contains("?") Or arg.Keys.Contains("help") Then
                Console.Write(My.Resources.Syntax)
                If arg.Keys.Contains("w") Then Console.ReadKey()
                Environment.Exit(0)

            End If

            If Not arg.Keys.Contains("id") Then
                Console.WriteLine("Missing ID parameter. Please see -help.")
                If arg.Keys.Contains("w") Then Console.ReadKey()
                Environment.Exit(400)
            End If

            Dim sql As SqlCommand = Nothing

            ' Find the local Priority sql database
            Dim db As ServerInstance = SearchInstance()

            ' Load module data from offsite web server
            Dim mv As modver.modver = modData()
            Console.WriteLine("Ok.")

            ' Open the database
            Console.Write("Connecing to [{0}]...", db.ConnectionString)
            Using cn As New SqlConnection(db.ConnectionString)
                cn.Open()
                Console.WriteLine("Ok.")

                With mv
                    For Each m As modver.mModule In mv.module

                        MVer.Add(m.name, 0)

                        For Each v As modver.mVersion In m.version
                            Console.WriteLine("Verifying Module [{0}] version [{1}]...", m.name, v.number.ToString)

                            Dim pass As Boolean = True
                            For Each ch As modver.mCheck In v.check
                                Select Case ch.type.ToLower
                                    Case "table"
                                        Console.Write("Check TABLE [{0}]...", ch.name)
                                        sql = New SqlCommand(
                                            String.Format(
                                                "use [{0}]; " &
                                                "select top 1 object_id from sys.tables where name = '{1}'",
                                                PriorityEnviroments(0),
                                                ch.name
                                            ), cn
                                        )
                                        Dim result = sql.ExecuteScalar
                                        If result Is Nothing Then
                                            pass = False
                                            Console.WriteLine("Fail")
                                            Exit For
                                        Else
                                            Console.WriteLine("OK")
                                        End If

                                        For Each col As modver.sqlCheck In ch.check
                                            Console.Write("Check TABLE Column [{0}.{1}]...", ch.name, col.name)
                                            sql = New SqlCommand(
                                                String.Format(
                                                    "use [{0}]; " &
                                                    "SELECT COUNT(*) FROM sys.all_columns WHERE oBject_id = {1} AND NAME = '{2}'",
                                                    PriorityEnviroments(0),
                                                    result,
                                                    col.name
                                                ), cn
                                            )
                                            If Not (sql.ExecuteScalar = 1) Then
                                                pass = False
                                                Console.WriteLine("Fail")
                                                Exit For
                                            Else
                                                Console.WriteLine("OK")
                                            End If

                                        Next

                                        If Not pass Then Exit For

                                    Case "form"
                                        Console.Write("Check FORM [{0}]...", ch.name)
                                        sql = New SqlCommand(
                                            String.Format(
                                                "use [system]; " &
                                                "select T$EXEC from T$EXEC where TYPE = 'F' and ENAME = '{0}'",
                                                ch.name
                                            ), cn
                                        )
                                        Dim result = sql.ExecuteScalar
                                        If result Is Nothing Then
                                            pass = False
                                            Console.WriteLine("Fail")
                                            Exit For
                                        Else
                                            Console.WriteLine("OK")
                                        End If

                                        For Each col As modver.sqlCheck In ch.check
                                            Console.Write("Check FORM Column [{0}.{1}]...", ch.name, col.name)
                                            sql = New SqlCommand(
                                                String.Format(
                                                    "use [system]; " &
                                                    "select count(*) from FORMCLMNS where FORM = {0} and NAME = '{1}'",
                                                    result,
                                                    col.name
                                                ), cn
                                            )
                                            If Not (sql.ExecuteScalar = 1) Then
                                                pass = False
                                                Console.WriteLine("Fail")
                                                Exit For
                                            Else
                                                Console.WriteLine("OK")
                                            End If

                                        Next

                                        If Not pass Then Exit For

                                    Case "sql"
                                        For Each col As modver.sqlCheck In ch.check
                                            Try
                                                If Not (ch.name.Length > 0 And col.name.Length > 0) Then
                                                    Throw New Exception("Missing sql trigger definition.")
                                                End If


                                                Select Case col.sql.Length
                                                    Case 0
                                                        Console.Write("Check for trigger [{0}\{1}]...", ch.name, col.name)
                                                        sql = New SqlCommand(
                                                            String.Format(
                                                                "use [{0}]; " &
                                                                "SELECT COUNT(*) AS Matching " &
                                                                "   FROM dbo.FORMTRIGTEXT " &
                                                                "INNER JOIN dbo.TRIGGERS ON dbo.FORMTRIGTEXT.TRIG = dbo.TRIGGERS.TRIG " &
                                                                "INNER JOIN dbo.T$EXEC ON dbo.FORMTRIGTEXT.FORM = dbo.T$EXEC.T$EXEC " &
                                                                "WHERE 0=0 " &
                                                                "   and (dbo.T$EXEC.ENAME = N'{1}') " &
                                                                "   AND (dbo.TRIGGERS.TRIGNAME = N'{2}') ",
                                                                "system",
                                                                ch.name,
                                                                col.name
                                                            ), cn
                                                        )

                                                    Case Else
                                                        Console.Write("Check for SQL in [{0}\{1}]...", ch.name, col.name)
                                                        sql = New SqlCommand(
                                                            String.Format(
                                                                "use [{0}]; " &
                                                                "SELECT COUNT(*) AS Matching " &
                                                                "   FROM dbo.FORMTRIGTEXT " &
                                                                "INNER JOIN dbo.TRIGGERS ON dbo.FORMTRIGTEXT.TRIG = dbo.TRIGGERS.TRIG " &
                                                                "INNER JOIN dbo.T$EXEC ON dbo.FORMTRIGTEXT.FORM = dbo.T$EXEC.T$EXEC " &
                                                                "WHERE 0=0 " &
                                                                "   and (dbo.T$EXEC.ENAME = N'{1}') " &
                                                                "   AND (dbo.TRIGGERS.TRIGNAME = N'{2}') " &
                                                                "   AND (dbo.FORMTRIGTEXT.TEXT LIKE N'%{3}%')",
                                                                "system",
                                                                ch.name,
                                                                col.name,
                                                                col.sql
                                                            ), cn
                                                        )

                                                End Select

                                                Dim result = sql.ExecuteScalar
                                                If result Is Nothing Then
                                                    pass = False
                                                    Console.WriteLine("Fail")
                                                    Exit For
                                                End If

                                                If Not (result = 1) Then
                                                    pass = False
                                                    Console.WriteLine("Fail")
                                                    Exit For
                                                End If

                                            Catch ex As Exception
                                                Console.WriteLine("Fail")
                                                Console.WriteLine(
                                                    String.Format(
                                                        "SQL error: {0}.",
                                                            ex.Message
                                                      )
                                                  )
                                                pass = False
                                                Exit For

                                            End Try

                                        Next

                                        If Not pass Then Exit For

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

            SaveResult()
            Environment.Exit(0)

        Catch ex As Exception
            Console.WriteLine(ex.Message)
            Environment.Exit(400)

        Finally
            If arg.Keys.Contains("w") Then Console.ReadKey()

        End Try



    End Sub

#Region "Private Methods"

    Private Function SearchInstance() As ServerInstance

        Console.Write("Locating SQL server...")
        For Each s As ServerInstance In EnumerateServers()
            If AttemptConnect(s) Then
                Console.WriteLine("Ok.")
                Console.WriteLine("Found instance [{0}].", s.ConnectionString)
                Console.Write("Checking Priority Environments...")
                CheckEnvironment(s)
                Console.WriteLine("Ok.")
                Return s
            End If
        Next

        Throw New Exception("No local Priority database.")

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
                    String.Format("{0}/modver.ashx", modverURL)
                )
            )
            Console.WriteLine("OK.")

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

    Private Sub SaveResult()

        Dim str As New Text.StringBuilder
        Using xr As XmlWriter = XmlWriter.Create(str)

            xr.WriteStartDocument()
            xr.WriteStartElement("modver")
            xr.WriteAttributeString("id", arg("id").ToString)

            For Each m As String In MVer.Keys
                xr.WriteStartElement("module")
                xr.WriteAttributeString("version", MVer(m).ToString)
                xr.WriteAttributeString("name", m.ToString)
                xr.WriteEndElement()

            Next

            xr.WriteEndElement()
            xr.WriteEndDocument()

        End Using

        Try
            Dim requestStream As Stream = Nothing
            Dim uploadResponse As Net.HttpWebResponse = Nothing
            Dim uploadRequest As Net.HttpWebRequest = CType(Net.HttpWebRequest.Create(String.Format("{0}/modver.ashx", modverURL)), Net.HttpWebRequest)
            uploadRequest.Method = "POST"
            uploadRequest.Proxy = Nothing

            Dim myEncoder As New System.Text.ASCIIEncoding
            Dim ms As MemoryStream = New MemoryStream(myEncoder.GetBytes(str.ToString))

            requestStream = uploadRequest.GetRequestStream()

            ' Upload the XML
            Dim buffer(1024) As Byte
            Dim bytesRead As Integer
            While True
                bytesRead = ms.Read(buffer, 0, buffer.Length)
                If bytesRead = 0 Then
                    Exit While
                End If
                requestStream.Write(buffer, 0, bytesRead)
            End While

            requestStream.Close()
            uploadResponse = uploadRequest.GetResponse()

            With uploadResponse
                If uploadResponse.StatusCode = Net.HttpStatusCode.OK Then
                    Console.WriteLine("Ok.")

                Else
                    Throw New Exception(
                        String.Format(
                            "Server error {0}: {1}",
                            .StatusCode,
                            .StatusDescription
                        )
                   )

                End If
            End With

        Catch ex As Exception
            Throw New Exception(
                String.Format(
                    "Failed: {0}",
                    ex.Message
                )
            )

        End Try


    End Sub


#End Region

End Module
