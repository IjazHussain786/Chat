﻿namespace Simple_Chat.Hubs
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNet.SignalR;
    using System;
    using Providers;
    using BLL;
    using BLL.Models;
    using System.Threading.Tasks;

    public class ChatHub : Hub
    {
        private static List<UserModel> ActiveUsers = new List<UserModel>();
        static string uname;
        static bool login = false;
        static string condition = "";
        ManagerAsync manager = new ManagerAsync();

        public async Task Starting(string username, string roomname)
        {
            var id = Context.ConnectionId;
            UserModel user = null;

            if (!ReferenceEquals(username, null) && username != "")
                user = await manager.GetUserByName(username);
            if (condition == "join")
            {

                if (!(await manager.RoomContainsUser(username, roomname)))
                {
                    Clients.AllExcept(id).onNewRoomConnect(id, username, roomname);
                    await JoinGroup(username, roomname);
                }

                ActiveUsers.Where(p => p.UserName == username).First().ConnectionId = id;
                var users = await manager.GetUsersByRoom(roomname);
                Clients.Client(id).onRoomConnected(id, username, users);
                condition = "";
                var messages = await manager.GetMessagesByRoomName(roomname);
                Clients.Client(id).showAllMessages(roomname, messages);
            }
            else
            if (ActiveUsers.Where(p => p.UserName == username).Count() == 1 && uname != null && !ReferenceEquals(user, null) && condition == "")
            {
                if (user.active)
                {
                    ActiveUsers.Where(p => p.UserName == uname).First().ConnectionId = id;

                    var jr =await manager.GetRoomsByUser(username);
                    var rooms =await manager.GetRooms(username);

                    Clients.Client(id).onConnected(Context.ConnectionId, username, ActiveUsers, jr, rooms);
                    if (login)
                    {
                        Clients.AllExcept(id).onNewUserConnected(id, username, roomname);
                        login = false;
                    }
                    var messages = await manager.GetMessagesByRoomName("general");
                    Clients.Client(id).showAllMessages("general", messages);

                }

            }
            else if (!ReferenceEquals(user, null) && username != null)
            {
                if (user.active == false)
                {
                    Clients.Client(id).onLoginFail();
                    login = false;
                }
            }
        }
        public async Task Send(string name, string message)
        {
            var guid = Guid.NewGuid();
            await manager.InsertMessage(guid, name, "general", message);
            Clients.All.addMessage(await manager.GetMessageByID(guid));
        }
        public async Task SendToGroup(string username, string message, string roomname)
        {
            var guid = Guid.NewGuid();
            await manager.InsertMessage(guid, username, roomname, message);
            Clients.All.addMessageToRoom(await manager.GetMessageByID(guid));
            var usersfornotify = await manager.GetUsersByRoom(roomname);
            List<string> notify = new List<string>();
            foreach (var m in usersfornotify)
            {
                if (ActiveUsers.Contains(m))
                    notify.Add(ActiveUsers.FirstOrDefault(p => p.UserName == m.UserName).ConnectionId);
            }
            Clients.Clients(notify).desktopNot(roomname, username, message);
        }
        public async Task EditMessage(string id, string newmessage)
        {
            await manager.EditMessage(Guid.Parse(id), newmessage);
            Clients.All.onEditingMsg(id, newmessage);
        }

        public async Task Connect(string username, string password)
        {
            var id = Context.ConnectionId;
            uname = username;
            login = true;
            if (ActiveUsers.All(x => x.ConnectionId != id) && ActiveUsers.All(x => x.UserName != username))
            {
                if (await manager.Login(username, password) && (await manager.GetUserByName(username)).active)
                {
                    ActiveUsers.Add(new UserModel() { ConnectionId = id, UserName = username, Password = password });
                    Clients.Caller.onLogin();
                }
                else
                {
                    Clients.Caller.onLoginFail();
                }
            }
            else
            {
                Clients.Caller.onLoginFail();
            }
        }
        public async Task Disconnect(string condition, string p)
        {
            var id = Context.ConnectionId;
            await base.OnDisconnected(true);
            if (condition == "join")
            {
                ChatHub.condition = condition;
            }
        }
        public async Task Create(string group)
        {
            if (await manager.IsValidRoomName(group))
            {
                await manager.InsertRoom(group);
                Clients.All.onNewGroupCreating(group);
            }
            else
            {
                Clients.Caller.failedRoomCreating();
            }
        }
        public async Task JoinGroup(string username, string roomname)
        {
            await manager.JoinGroup(username, roomname);
            condition = "join";
        }
        public async Task OutFromRoom(string username, string roomname)
        {
            await manager.OutFromRoom(username, roomname);
            Clients.All.outFromRoom(username);
            Clients.All.onRoomOut(username, roomname);
        }
        public async Task OutButton(string username, string roomname)
        {
            await manager.OutFromRoom(username, roomname);
            Clients.Caller.outRoomButton(roomname);
            Clients.All.onRoomOut(username, roomname);
        }
        public async Task ToGeneral(string username, string roomname)
        {
            await Clients.Caller.ToGeneral(username);
        }
        public async Task GetHistory(string id)
        {
            List<HistoryModel> history = await manager.GetHistory(id);
            Clients.Caller.onCallingHistory(history);
        }
        public async Task SubmitRegistration(string username, string password, string email)
        {
            string tok = Guid.NewGuid().ToString();
            bool t;
            if (await manager.IsValidNameOrEmail(username, email) && username.Length < 20 && password.Length >= 6)
            {
                MailProvider.SendMail(email, tok);
                await manager.InsertUser(username, email, password, tok, false);
                t = true;
            }
            else
            {
                t = false;
            }
            Clients.Caller.onRegistration(t);
        }
        public async Task LogOut(string username)
        {
            ActiveUsers.Remove(ActiveUsers.FirstOrDefault(p => p.UserName == username));
            await Clients.Caller.onLogOut();
            await Clients.All.onUserDisconnected(username);
            manager.LogOut();
        }
    }
}