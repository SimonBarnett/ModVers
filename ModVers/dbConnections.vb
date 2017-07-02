Imports System.Data.Sql
Imports System.Data.SqlClient
Imports System.IO

Public Module dbConnections

#Region "Public Properties"

    Private _sqlServer As ServerInstance = Nothing
    Public ReadOnly Property sqlServer() As ServerInstance
        Get
            Return _sqlServer
        End Get
    End Property

    Private _PriorityEnv As New List(Of String)
    Public ReadOnly Property PriorityEnviroments() As List(Of String)
        Get
            Return _PriorityEnv
        End Get
    End Property

#End Region

#Region "Public Methods"

    Public Function EnumerateServers() As List(Of ServerInstance)

        Dim slist As New List(Of ServerInstance)
        Dim tableServers As DataTable = Nothing
        Dim instance As SqlDataSourceEnumerator =
            SqlDataSourceEnumerator.Instance

        ' Build the list of servers.
        For Each row As System.Data.DataRow In instance.GetDataSources().Rows
            If (
                String.Compare(Environment.MachineName, row("ServerName").ToString()) = 0
                ) _
            Then
                slist.Add(
                    New ServerInstance(
                        row("ServerName").ToString(),
                        row("InstanceName").ToString()
                    )
                )
            End If
        Next
        Return slist

    End Function

    Public Function AttemptConnect(ByRef sqlInstance As ServerInstance) As Boolean
        Using Connection As New SqlConnection(sqlInstance.ConnectionString)
            Connection.Open()
            Dim cmd As New SqlCommand(
                "SELECT count(name)" &
                "FROM sysdatabases " &
                "where name in ('system','pritempdb')",
                Connection
            )
            Select Case cmd.ExecuteScalar
                Case 2
                    _sqlServer = sqlInstance
                    Return True
                Case Else
                    Return False
            End Select
        End Using
    End Function

    Public Sub CheckEnvironment(ByRef sqlInstance As ServerInstance)
        _PriorityEnv.Clear()
        Using Connection As New SqlConnection(sqlInstance.ConnectionString)
            Connection.Open()
            Dim cmd As New SqlCommand(
                "use [system] " &
                "select DNAME from ENVIRONMENT " &
                "where DNAME <> ''",
                Connection
            )
            Using reader As SqlDataReader = cmd.ExecuteReader
                While reader.Read
                    If Not _PriorityEnv.Contains(reader(0)) Then
                        _PriorityEnv.Add(reader(0))
                    End If
                End While
            End Using
        End Using
    End Sub

#End Region

End Module
