using Bot.CommandSystem;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Bot
{
    class GroupKeepAlive : BaseCommands, IProgram
    {
        public string ProgramName => "Group KeepAlive Daemon";

        public float ProgramVersion => 1.0f;

        public void getTick()
        {
            // Check groups and request join if not in chat
            foreach(KeyValuePair<UUID, Group> groups in GroupsCache)
            {
                if (BotSession.Instance.grid.Self.GroupChatSessions.ContainsKey(groups.Key))
                {
                    // OK
                }
                else
                {
                    BotSession.Instance.grid.Self.RequestJoinGroupChat(groups.Key);
                    MHE(Destinations.DEST_LOCAL, UUID.Zero, "I lost the group chat session for secondlife:///app/group/" + groups.Key.ToString() + "/about - Attempting to rejoin the group chat");
                }
            }
        }

        public void LoadConfiguration()
        {
            // No configuration for this plugin
        }

        public void onIMEvent(object sender, InstantMessageEventArgs e)
        {
            BotSession.Instance.grid.Self.IM -= onIMEvent;
        }

        public void passArguments(string data)
        {
        }


        private static void CacheGroupRoles(object sender, GroupRolesDataReplyEventArgs e)
        {
            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] role_reply");
            if (!Directory.Exists("zGroupCache")) Directory.CreateDirectory("zGroupCache"); // this should be purged at every bot restart!!!

            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] generating groupcache file");
            zGroupCaches newCache = new zGroupCaches();
            zGroupCaches.GroupMemoryData gmd = new zGroupCaches.GroupMemoryData();
            foreach (KeyValuePair<UUID, GroupRole> roleData in e.Roles)
            {
                gmd.roleID = roleData.Value.ID;
                gmd.RoleName = roleData.Value.Name;
                gmd.Title = roleData.Value.Title;
                gmd.Powers = roleData.Value.Powers;


                newCache.GMD.Add(gmd);

            }
            newCache.GroupID = e.GroupID;
            newCache.Save(e.GroupID.ToString());
            RoleReply.Set();
            FileInfo fi = new FileInfo("GroupCache/" + e.GroupID.ToString() + ".json");

            //MHE(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "[debug] Roles for secondlife:///app/group/" + e.GroupID.ToString() + "/about have been saved to: GroupCache/" + e.GroupID.ToString() + ".bdf\nFileSize: "+fi.Length.ToString(), 55);


        }
        private static Dictionary<UUID, Group> GroupsCache = null;
        private static ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        private static ManualResetEvent RoleReply = new ManualResetEvent(false);
        private static void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();

            foreach (KeyValuePair<UUID, Group> DoCache in GroupsCache)
            {
                bool Retry = true;
                int count = 0;
                while (Retry)
                {
                    BotSession.Instance.grid.Groups.RequestGroupRoles(DoCache.Value.ID);
                    if (RoleReply.WaitOne(TimeSpan.FromSeconds(30), false)) { Retry = false; }
                    else
                    {
                        count++;
                        //MH.callbacks(MessageHandler.Destinations.DEST_LOCAL, UUID.Zero, "There appears to have been a failure requesting the group roles for secondlife:///app/group/" + DoCache.Value.ID.ToString() + "/about - Trying again");

                        if (count >= 5)
                        {
                            MH(Destinations.DEST_LOCAL, UUID.Zero, "Aborting group refresh attempt. Too many errors - Resetting cache and retrying");
                            GroupsEvent.Reset();
                            GroupsCache = new Dictionary<UUID, Group>();
                            BotSession.Instance.grid.Groups.CurrentGroups -= Groups_CurrentGroups;

                            ReloadGroupsCache();

                            return;
                        }

                    }
                }
            }
        }
        private static void ReloadGroupsCache()
        {
            BotSession.Instance.grid.Groups.CurrentGroups += Groups_CurrentGroups;
            BotSession.Instance.grid.Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            BotSession.Instance.grid.Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        private UUID GroupName2UUID(String groupName)
        {
            UUID tryUUID;
            if (UUID.TryParse(groupName, out tryUUID))
                return tryUUID;
            if (null == GroupsCache)
            {
                ReloadGroupsCache();
                if (null == GroupsCache)
                    return UUID.Zero;
            }
            lock (GroupsCache)
            {
                if (GroupsCache.Count > 0)
                {
                    foreach (Group currentGroup in GroupsCache.Values)
                        if (currentGroup.Name.ToLower() == groupName.ToLower())
                            return currentGroup.ID;
                }
            }
            return UUID.Zero;
        }

        private static bool IsGroup(UUID grpKey)
        {
            // For use in IMs since it appears partially broken at the moment
            return GroupsCache.ContainsKey(grpKey);
        }

        public void run()
        {
            MHE(Destinations.DEST_LOCAL, UUID.Zero, $"Plugin [{ProgramName}]: {ProgramVersion} has been initialized");

            BotSession.Instance.grid.Groups.CurrentGroups += Groups_CurrentGroups;
            BotSession.Instance.grid.Groups.GroupRoleDataReply += CacheGroupRoles;

            ReloadGroupsCache();
        }
    }
}
