open System
open System.Linq
open ServiceStack.ServiceHost
open ServiceStack.WebHost.Endpoints
open ServiceStack.ServiceInterface
open ServiceStack.ServiceInterface.Auth
open ServiceStack.CacheAccess
open ServiceStack.CacheAccess.Providers
open ServiceStack.OrmLite

type MessageModel() = 
    member val Id = -1 with get, set
    member val UserId = -1 with get, set
    member val Text = "" with get, set
    member val DateTime = DateTime.Now with get, set
    member val ChatId = -1 with get, set

type ChatModel() = 
    member val Id = -1 with get, set
    member val FirstUserId = -1 with get, set
    member val SecondUserId = -1 with get, set

type ContactModel() = 
    member val Id = -1 with get, set
    member val Image = "" with get, set
    member val FullName = "" with get, set

type MessageResponseModel() = 
    member val FullName = "" with get, set
    member val Image = "" with get, set
    member val Text = "" with get, set
    member val DateTime = DateTime.Now with get, set

type IDataRepository = 
    abstract Contacts: ResizeArray<ContactModel>
    abstract GetMessages: int * int -> ResizeArray<MessageResponseModel>
    abstract SaveMessage: string * int * int -> unit
    abstract GetProfile: int -> string * string
    abstract SetProfile: int * string * string -> unit

type OrmLiteDataRepository(connectionFactory: IDbConnectionFactory) = 
    let GetChatId(firstUserId, secondUserId) = 
        use connection = connectionFactory.OpenDbConnection()
        let chats = connection.Select<ChatModel>().Where(fun c -> c.FirstUserId = firstUserId && c.SecondUserId = secondUserId ||
                                                             c.SecondUserId = firstUserId && c.FirstUserId = secondUserId).ToList()
        let chatId = 
            match chats with
            | _ when chats.Count = 0 -> connection.Insert(new ChatModel(FirstUserId = firstUserId, SecondUserId = secondUserId))
                                        int (connection.GetLastInsertId())
            | _ -> chats.[0].Id
        chatId

    interface IDataRepository with
        member this.Contacts = 
            use connection = connectionFactory.OpenDbConnection()
            connection.Select<ContactModel>("SELECT Id, DisplayName AS Image, FullName FROM UserAuth")

        member this.GetMessages(firstUserId, secondUserId) = 
            let chatId = GetChatId(firstUserId, secondUserId)
            use connection = connectionFactory.OpenDbConnection()
            let firstUserAuth = connection.GetById<UserAuth>(firstUserId)
            let secondUserAuth = connection.GetById<UserAuth>(secondUserId)
            let messages = connection.Select<MessageModel>().Where(fun m -> m.ChatId = chatId).OrderBy(fun m -> m.DateTime)
            let result = new ResizeArray<MessageResponseModel>()
            for message in messages do
                let responseMessage = new MessageResponseModel()
                responseMessage.Text <- message.Text
                responseMessage.DateTime <- message.DateTime
                responseMessage.FullName <- if message.UserId = firstUserAuth.Id then firstUserAuth.FullName else secondUserAuth.FullName
                responseMessage.Image <- if message.UserId = firstUserAuth.Id then firstUserAuth.DisplayName else secondUserAuth.DisplayName
                result.Add(responseMessage)
            result
            
        member this.SaveMessage(message, sender, receiver) = 
            let chatId = GetChatId(sender, receiver)
            use connection = connectionFactory.OpenDbConnection()
            connection.Insert(new MessageModel(UserId = sender, Text = message, DateTime = DateTime.Now, ChatId = chatId))
            // Todo: push notification

        member this.GetProfile(userId) = 
            use connection = connectionFactory.OpenDbConnection()
            let userAuth = connection.GetById<UserAuth>(userId)
            userAuth.DisplayName, userAuth.FullName

        member this.SetProfile(userId, image, fullName) = 
            use connection = connectionFactory.OpenDbConnection()
            connection.UpdateOnly(new UserAuth(Id = userId, DisplayName = image), fun (ua: UserAuth) -> ua.DisplayName) |> ignore
            connection.UpdateOnly(new UserAuth(Id = userId, FullName = fullName), fun (ua: UserAuth) -> ua.FullName) |> ignore  

type BaseResponse() = 
    member val Result = "" with get, set

type ProfileResponse() =
    inherit BaseResponse()
    member val Image = "" with get, set
    member val FullName = "" with get, set

[<Authenticate>]
[<Route("/profile")>]
type Profile() = 
    interface IReturn<ProfileResponse>
    member val Image = "" with get, set
    member val FullName = "" with get, set

type ChatResponse() = 
    inherit BaseResponse()
    member val Messages = new ResizeArray<MessageResponseModel>() with get, set

[<Authenticate>]
[<Route("/chat")>]
type Chat() = 
    interface IReturn<ChatResponse>
    member val UserId = -1 with get, set
    member val Message = "" with get, set

type ContactsResponse() = 
    inherit BaseResponse()
    member val Contacts = new ResizeArray<ContactModel>() with get, set

[<Authenticate>]
[<Route("/contacts")>]
type Contacts() = interface IReturn<ContactsResponse>

type MessengerService() =
    inherit Service()
    member this.Post (req: Profile) = 
        let dataRepository: IDataRepository = this.TryResolve()
        dataRepository.SetProfile(int (this.GetSession().UserAuthId), req.Image, req.FullName)
        new ProfileResponse()

    member this.Get (req: Profile) =
        let dataRepository: IDataRepository = this.TryResolve()
        let image, fullName = dataRepository.GetProfile(int (this.GetSession().UserAuthId))
        new ProfileResponse(Image = image, FullName = fullName)

    member this.Post (req: Chat) = 
        let dataRepository: IDataRepository = this.TryResolve()
        dataRepository.SaveMessage(req.Message, int (this.GetSession().UserAuthId), req.UserId)
        new ChatResponse(Messages = null)

    member this.Get (req: Chat) = 
        let dataRepository: IDataRepository = this.TryResolve()
        new ChatResponse(Messages = dataRepository.GetMessages(int (this.GetSession().UserAuthId), req.UserId))

    member this.Get (req: Contacts) = 
        let dataRepository: IDataRepository = this.TryResolve()
        new ContactsResponse(Contacts = dataRepository.Contacts)

type AppHost =
    inherit AppHostHttpListenerBase
    new() = { inherit AppHostHttpListenerBase("messenger", typeof<MessengerService>.Assembly) }
    override this.Configure container = 
        let authFeature = new AuthFeature((fun () -> new AuthUserSession() :> IAuthSession), [| new CredentialsAuthProvider() |])
        authFeature.IncludeAssignRoleServices <- false
        this.Plugins.Add(authFeature)
        this.Plugins.Add(new RegistrationFeature())
        container.Register<ICacheClient>(new MemoryCacheClient())
        let connectionFactory = new OrmLiteConnectionFactory("messenger.db", SqliteDialect.Provider)
        container.Register<IDataRepository>(new OrmLiteDataRepository(connectionFactory))
        container.Register<IDbConnectionFactory>(connectionFactory)
        let authRepository = new OrmLiteAuthRepository(connectionFactory)
        authRepository.CreateMissingTables()
        container.Register<IUserAuthRepository>(authRepository)
        use connection = connectionFactory.OpenDbConnection()
        connection.CreateTableIfNotExists<MessageModel>()
        connection.CreateTableIfNotExists<ChatModel>()

[<EntryPoint>]
let main args =
    let host = if args.Length = 0 then "http://127.0.0.1:8080/" else args.[0]
    printfn "Listening on %s ..." host
    let appHost = new AppHost()
    appHost.Init()
    appHost.Start host
    Console.ReadLine() |> ignore
    0