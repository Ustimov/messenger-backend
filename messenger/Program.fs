open System
open System.Linq
open System.IO
open System.Net
open System.Reflection
open System.Runtime.Serialization
open ServiceStack.ServiceHost
open ServiceStack.WebHost.Endpoints
open ServiceStack.ServiceInterface
open ServiceStack.ServiceInterface.Auth
open ServiceStack.CacheAccess
open ServiceStack.CacheAccess.Providers
open ServiceStack.OrmLite
open ServiceStack.DataAnnotations
open ServiceStack.Common.Web

type MessageModel() = 
    [<AutoIncrement>]
    member val Id = -1 with get, set
    member val UserId = -1 with get, set
    member val Text = "" with get, set
    member val DateTime = DateTime.Now with get, set
    member val ChatId = -1 with get, set

type ChatModel() = 
    [<AutoIncrement>]
    member val Id = -1 with get, set
    member val FirstUserId = -1 with get, set
    member val SecondUserId = -1 with get, set

[<DataContract(Name = "ContactModel")>]
type ContactModel() = 
    [<DataMember>]
    member val Id = -1 with get, set
    [<DataMember>]
    member val Image = "" with get, set
    [<DataMember>]
    member val FullName = "" with get, set
    [<DataMember>]
    member val LastMessage = "" with get, set
    [<DataMember>]
    member val LastMessageDateTime = DateTime.MinValue with get, set

[<DataContract(Name = "MessageModel")>]
type MessageResponseModel() = 
    [<DataMember>]
    member val FullName = "" with get, set
    [<DataMember>]
    member val Image = "" with get, set
    [<DataMember>]
    member val Text = "" with get, set
    [<DataMember>]
    member val DateTime = DateTime.Now with get, set

type IDataRepository = 
    abstract GetContacts: int -> ResizeArray<ContactModel>
    abstract GetMessages: int * int -> ResizeArray<MessageResponseModel>
    abstract SaveMessage: string * int * int -> unit
    abstract GetProfile: int -> string * string
    abstract SetProfileFullName: int * string -> unit
    abstract SetProfileImage: int * string * Stream -> unit

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
        member this.GetContacts(userId) = 
            use connection = connectionFactory.OpenDbConnection()
            let contacts = connection.Select<ContactModel>("SELECT Id, DisplayName AS Image, FullName FROM UserAuth")
            let msgByChatId = connection.Select<MessageModel>().OrderByDescending(fun (m: MessageModel) -> m.DateTime)
                                                               .GroupBy(fun (m: MessageModel) -> m.ChatId)
            let chats = connection.Select<ChatModel>()
            for chat in chats do
                let contact = if chat.FirstUserId = userId then contacts.Find(fun c -> c.Id = chat.SecondUserId)
                                                           else contacts.Find(fun c-> c.Id = chat.FirstUserId)
                let group = msgByChatId.SingleOrDefault(fun c -> c.Key = chat.Id)
                if group <> null && group.Count() > 0 then
                    let lastMsg = group.First()
                    contact.LastMessage <- lastMsg.Text
                    contact.LastMessageDateTime <- lastMsg.DateTime
            contacts.OrderByDescending(fun (c: ContactModel) -> c.LastMessageDateTime).ToList()
            
        member this.GetMessages(firstUserId, secondUserId) = 
            let chatId = GetChatId(firstUserId, secondUserId)
            use connection = connectionFactory.OpenDbConnection()
            let firstUserAuth = connection.GetById<UserAuth>(firstUserId)
            let secondUserAuth = connection.GetById<UserAuth>(secondUserId)
            let messages = connection.Select<MessageModel>().Where(fun m -> m.ChatId = chatId)
                                                            .OrderByDescending(fun m -> m.DateTime).Take(20)
            let result = new ResizeArray<MessageResponseModel>()
            for message in messages do
                let responseMessage = new MessageResponseModel()
                responseMessage.Text <- message.Text
                responseMessage.DateTime <- message.DateTime
                responseMessage.FullName <- if message.UserId = firstUserAuth.Id then firstUserAuth.FullName 
                                                                                 else secondUserAuth.FullName
                responseMessage.Image <- if message.UserId = firstUserAuth.Id then firstUserAuth.DisplayName
                                                                              else secondUserAuth.DisplayName
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

        member this.SetProfileFullName(userId, fullName) = 
            use connection = connectionFactory.OpenDbConnection()
            let userAuth = connection.GetById<UserAuth>(userId)
            userAuth.FullName <- fullName
            if userAuth.DisplayName = null then userAuth.DisplayName <- "image/DefaultImage.png"
            connection.Update(userAuth, fun (ua: UserAuth) -> ua.Id = userId) |> ignore

        member this.SetProfileImage(userId, fileName, stream) = 
            use connection = connectionFactory.OpenDbConnection()
            let userAuth = connection.GetById<UserAuth>(userId)
            let imagePath = sprintf "image/%d-%s" userId fileName
            use fileStream = new FileStream(imagePath, FileMode.Create)
            stream.CopyTo(fileStream)
            userAuth.DisplayName <- imagePath
            connection.Update(userAuth, fun (ua: UserAuth) -> ua.Id = userId) |> ignore

[<DataContract(Name = "ProfileResponse")>]
type ProfileResponse() =
    [<DataMember>]
    member val Image = "" with get, set
    [<DataMember>]
    member val FullName = "" with get, set

[<Authenticate>]
[<DataContract(Name = "Profile")>]
[<Route("/profile")>]
type Profile() = 
    interface IReturn<ProfileResponse>
    [<DataMember>]
    member val Image = "" with get, set
    [<DataMember>]
    member val FullName = "" with get, set

[<DataContract(Name = "ChatResponse")>]
type ChatResponse() = 
    [<DataMember>]
    member val Messages = new ResizeArray<MessageResponseModel>() with get, set

[<Authenticate>]
[<DataContract(Name = "Chat")>]
[<Route("/chat")>]
type Chat() = 
    interface IReturn<ChatResponse>
    [<DataMember>]
    member val UserId = -1 with get, set
    [<DataMember>]
    member val Message = "" with get, set

[<DataContract(Name = "ContactsResponse")>]
type ContactsResponse() = 
    [<DataMember>]
    member val Contacts = new ResizeArray<ContactModel>() with get, set

[<Authenticate>]
[<DataContract(Name = "Contacts")>]
[<Route("/contacts")>]
type Contacts() = interface IReturn<ContactsResponse>

[<Authenticate>]
[<DataContract(Name = "Image")>]
[<Route("/upload")>]
type Image() = interface IReturn<HttpResult>

type MessengerService() =
    inherit Service()
    member this.Post (req: Profile) = 
        let dataRepository: IDataRepository = this.TryResolve()
        dataRepository.SetProfileFullName(int (this.GetSession().UserAuthId), req.FullName)
        new HttpResult(HttpStatusCode.OK, "Ok")

    member this.Get (req: Profile) =
        let dataRepository: IDataRepository = this.TryResolve()
        let image, fullName = dataRepository.GetProfile(int (this.GetSession().UserAuthId))
        new ProfileResponse(Image = image, FullName = fullName)

    member this.Post (req: Chat) = 
        let dataRepository: IDataRepository = this.TryResolve()
        dataRepository.SaveMessage(req.Message, int (this.GetSession().UserAuthId), req.UserId)
        new HttpResult(HttpStatusCode.OK, "Ok")

    member this.Get (req: Chat) = 
        let dataRepository: IDataRepository = this.TryResolve()
        new ChatResponse(Messages = dataRepository.GetMessages(int (this.GetSession().UserAuthId), req.UserId))

    member this.Get (req: Contacts) = 
        let dataRepository: IDataRepository = this.TryResolve()
        new ContactsResponse(Contacts = dataRepository.GetContacts(int (this.GetSession().UserAuthId)))
   
    member this.Post(req: Image) = 
        if this.Request.Files.Count() = 1 then
            let image = this.Request.Files.[0]
            let dataRepository: IDataRepository = this.TryResolve()
            dataRepository.SetProfileImage(int (this.GetSession().UserAuthId), image.FileName, image.InputStream)
        new HttpResult(HttpStatusCode.OK, "Ok")   

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

let setupResources = 
    Directory.CreateDirectory("image") |> ignore
    use defaultImage = Assembly.GetExecutingAssembly().GetManifestResourceStream("DefaultImage.png")
    use imageFileStream = new FileStream("image/DefaultImage.png", FileMode.Create, FileAccess.Write)
    defaultImage.CopyTo(imageFileStream)
    use sqliteDll = Assembly.GetExecutingAssembly().GetManifestResourceStream("sqlite3.dll")
    use sqliteFileStream = new FileStream("sqlite3.dll", FileMode.Create, FileAccess.Write)
    sqliteDll.CopyTo(sqliteFileStream)

[<EntryPoint>]
let main args =
    setupResources
    let host = if args.Length = 0 then "http://127.0.0.1:9000/" else args.[0]
    printfn "Listening on %s ..." host
    let appHost = new AppHost()
    appHost.Init()
    appHost.Start host
    Console.ReadLine() |> ignore
    0