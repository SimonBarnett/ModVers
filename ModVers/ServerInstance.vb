Public Class ServerInstance

#Region "Initialisation"

    Public Sub New(ByVal server As String, ByVal instance As String)
        m_server = server
        m_instance = instance
    End Sub

#End Region

#Region "Public Properties"

    Private m_server As String = ""
    Public Property Server() As String
        Get
            Return m_server
        End Get
        Set(ByVal value As String)
            m_server = value
        End Set
    End Property

    Private m_instance As String = ""
    Public Property Instance() As String
        Get
            Return m_instance
        End Get
        Set(ByVal value As String)
            m_instance = value
        End Set
    End Property

    Public ReadOnly Property ServerInstance() As String
        Get
            With Me
                Select Case .Instance.Length
                    Case 0
                        Return .Server
                    Case Else
                        Return String.Format("{0}\{1}", .Server, .Instance)
                End Select
            End With
        End Get
    End Property

    Public ReadOnly Property ConnectionString() As String
        Get
            Return String.Format(
                "Server={0};Trusted_Connection=True;",
                ServerInstance
            )
        End Get
    End Property

    Private _LoginException As Exception
    Public Property LoginException() As Exception
        Get
            Return _LoginException
        End Get
        Set(ByVal value As Exception)
            _LoginException = value
        End Set
    End Property

#End Region

End Class