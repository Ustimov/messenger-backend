# messenger

Simple messenger server for Data Structure course written in F# + ServiceStack.

## Features

* Registration

* Authentication

* User profile with image

* Contact list

* Private chats

* Message sending

## API

### Registration

Register new user.

#### POST /register

Request

```json
{"UserName": "String", "Password": "String"}
```

and response

```json
{"UserId":"String","SessionId":"String","UserName":"String","ReferrerUrl":"String","ResponseStatus":{"ErrorCode":"String","Message":"String","StackTrace":"String","Errors":[{"ErrorCode":"String","FieldName":"String","Message":"String"}]}}
```

or request

```xml
<Registration xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.servicestack.net/types">
  <UserName>String</UserName>
  <Password>String</Password>
</Registration>
```

and response

```xml
<RegistrationResponse xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.servicestack.net/types">
  <UserId>String</UserId>
  <SessionId>String</SessionId>
  <UserName>String</UserName>
  <ReferrerUrl>String</ReferrerUrl>
  <ResponseStatus>
    <ErrorCode>String</ErrorCode>
    <Message>String</Message>
    <StackTrace>String</StackTrace>
    <Errors>
      <ResponseError>
        <ErrorCode>String</ErrorCode>
        <FieldName>String</FieldName>
        <Message>String</Message>
      </ResponseError>
    </Errors>
  </ResponseStatus>
</RegistrationResponse>
```

### Authentication

Authenticate user with login and password.

#### POST /auth/credentials

Request

```json
{"UserName":"String","Password":"String","RememberMe":false}
```

and response

```json
{"SessionId":"String","UserName":"String","ReferrerUrl":"String","ResponseStatus":{"ErrorCode":"String","Message":"String","StackTrace":"String","Errors":[{"ErrorCode":"String","FieldName":"String","Message":"String"}]}}
```

or request

```xml
<Auth xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.servicestack.net/types">
  <UserName>String</UserName>
  <Password>String</Password>
  <RememberMe>false</RememberMe>
</Auth>
```

and response

```xml
<AuthResponse xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.servicestack.net/types">
  <SessionId>String</SessionId>
  <UserName>String</UserName>
  <ReferrerUrl>String</ReferrerUrl>
  <ResponseStatus>
    <ErrorCode>String</ErrorCode>
    <Message>String</Message>
    <StackTrace>String</StackTrace>
    <Errors>
      <ResponseError>
        <ErrorCode>String</ErrorCode>
        <FieldName>String</FieldName>
        <Message>String</Message>
      </ResponseError>
    </Errors>
  </ResponseStatus>
</AuthResponse>
```

### Logout

Logout user.

#### POST /auth/logout

Response

```json
{"SessionId":"String","UserName":"String","ReferrerUrl":"String","ResponseStatus":{"ErrorCode":"String","Message":"String","StackTrace":"String","Errors":[{"ErrorCode":"String","FieldName":"String","Message":"String"}]}}
```

or response

```xml
<AuthResponse xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.servicestack.net/types">
  <SessionId>String</SessionId>
  <UserName>String</UserName>
  <ReferrerUrl>String</ReferrerUrl>
  <ResponseStatus>
    <ErrorCode>String</ErrorCode>
    <Message>String</Message>
    <StackTrace>String</StackTrace>
    <Errors>
      <ResponseError>
        <ErrorCode>String</ErrorCode>
        <FieldName>String</FieldName>
        <Message>String</Message>
      </ResponseError>
    </Errors>
  </ResponseStatus>
</AuthResponse>
```

### Profile

Get image url and full name of user.

#### GET /profile

Response

```json
{"Image":"String","FullName":"String"}
```

or response

```xml
<ProfileResponse xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/">
  <FullName>String</FullName>
  <Image>String</Image>
</ProfileResponse>
```

Set user full name.

#### POST /profile

Request

```json
{"FullName":"String"}
```

or

```xml
<Profile xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/">
  <FullName>String</FullName>
</Profile>
```

and response HTTP status 200 Ok

### Image

Upload image as form-data.

#### POST /image

Request with image as multipart/form-data. Make sure you provide correct image name.

Response is HTTP status 200 Ok. Otherwise can be 417 Expectation Failed HTTP status if there is no attached image file or attached multiple files.

### Contacts

Get contact list.

#### GET /contacts

Response

```json
{"Contacts":[{"Id":0,"Image":"String","FullName":"String","LastMessage":"String","LastMessageDateTime":"\/Date(-62135596800000-0000)\/"}]}
```

or response

```xml
<ContactsResponse xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/">
  <Contacts>
    <ContactModel>
      <FullName>String</FullName>
      <Id>0</Id>
      <Image>String</Image>
      <LastMessage>String</LastMessage>
      <LastMessageDateTime>0001-01-01T00:00:00</LastMessageDateTime>
    </ContactModel>
  </Contacts>
</ContactsResponse>
```

### Chat

Get messages for chat with user.

#### GET /chat

Request

```json
{"UserId":0}
```

or request

```json
<Chat xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/">
  <UserId>0</UserId>
</Chat>
```

response

```json
{"Messages":[{"FullName":"String","Image":"String","Text":"String","DateTime":"\/Date(-62135596800000-0000)\/"}]}
```

or response

```xml
<ChatResponse xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/">
  <Messages>
    <MessageModel>
      <DateTime>0001-01-01T00:00:00</DateTime>
      <FullName>String</FullName>
      <Image>String</Image>
      <Text>String</Text>
    </MessageModel>
  </Messages>
</ChatResponse>
```

Send message to chat with user.

#### POST /chat

Request

```json
{"UserId":0,"Message":"String"}
```

or request

```json
<Chat xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/">
  <Message>String</Message>
  <UserId>0</UserId>
</Chat>
```

and response HTTP status 200 Ok

## Todo

* Push notifications

## License

The MIT License (MIT)
Copyright (c) 2016 Artem Ustimov

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.