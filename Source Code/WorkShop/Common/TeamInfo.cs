using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;

namespace WorkShop
{
    [XmlRoot(ElementName = "TeamInfo")]
    public class TeamInfo
    {
        #region private data
        private List<Team> teams = null;
        #endregion

        [XmlElement(ElementName = "Teams")]
        public List<Team> Teams
        {
            get { return teams; }
            set { teams = value; }
        }

        public TeamInfo()
        { 
            this.Teams = new List<Team>();
        }

        #region Public functionalities.
        /// <summary>
        /// This method will remove the id span inside the teams due to the team delection.
        /// </summary>
        private void rebuildTeamOrder()
        {
            int newId = 1;
            
            var queryInorder = from eachTeam in this.teams
                               orderby eachTeam.GroupChartOrderNumber ascending
                               where eachTeam.GroupChartOrderNumber != 0
                               select eachTeam;

            foreach (var teams in queryInorder)
            {
                teams.GroupChartOrderNumber = newId ++;
            }

            var queryInorderForNew = from eachTeamForNew in this.teams
                                     where eachTeamForNew.GroupChartOrderNumber == 0
                                     select eachTeamForNew;

            foreach (var teams in queryInorderForNew)
            {
                teams.GroupChartOrderNumber = newId++;
            }
        }


        public void SaveConfiguration(string _path)
        {
            FileStream fs = null;
            try
            {
                // rebuild the team order number before insert.
                this.rebuildTeamOrder();

                XmlSerializer xs = new XmlSerializer(typeof(TeamInfo));
                fs = new FileStream(_path, FileMode.Create, FileAccess.Write);
                xs.Serialize(fs, this);
                fs.Close();
            }
            catch
            {
                if (fs != null)
                    fs.Close();
            }
        }

        public static TeamInfo LoadConfiguration(string _path)
        {
            FileStream fs = null;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(TeamInfo));
                fs = new FileStream(_path, FileMode.Open, FileAccess.Read);
                TeamInfo teamInfo = (TeamInfo)xs.Deserialize(fs);
                fs.Close();
                return teamInfo;
            }
            catch 
            {
                if (fs != null)
                    fs.Close();

                return null;
            }
        }

        public Team GetGroupByName(string _groupName)
        {
            if (Teams.Count == 0)
            {
                return null;
            }
            foreach (Team g in Teams)
            {
                if (g.Name.Equals(_groupName))
                {
                    return g;
                }
            }
            return null;
        }

        public List<Member> GetAllTeamMembers()
        {
            if (Teams.Count == 0)
            {
                return null;
            }

            List<Member> Members = new List<Member>();
            
            foreach (Team g in Teams)
            {
                foreach (Member m in g.Members)
                {
                    Members.Add(m);
                }
            }
            return Members;
        }

        public Dictionary<String, String> GetAllEnabledTeamMembersToDictionaryThroughTeamName(String _teamName)
        {
            if (Teams.Count == 0)
            {
                return null;
            }

            Dictionary<String, String> teamMembers = new Dictionary<string, string>();
            foreach (Team group in Teams)
            {
                if (group.Name == _teamName)
                {
                    foreach (Member m in group.Members)
                    {
                        if (m.Enable && !teamMembers.ContainsKey(m.MemberName))
                        {
                            teamMembers.Add(m.MemberName, m.Role.ToString().ToUpper());
                        }
                    }
                }
            }
            return teamMembers;
        }

        public List<Team> GetAllGroups()
        {
            if (Teams.Count == 0)
            {
                return null;
            }

            List<Team> groups = new List<Team>();
            foreach (Team g in Teams)
            {
                groups.Add(g);
            }
            return groups;
        }

        public String[] GetAllTeamNamesInOrder()
        {
            if (Teams.Count == 0)
                return null;
            
            var emptyorder = from teams in this.Teams
                             where teams.GroupChartOrderNumber == 0
                             select teams;
            if (emptyorder.Count<Team>() != 0)
            {
                this.rebuildTeamOrder();
            }
            
            var teamsNameInOrder = from teams in this.Teams
                                   orderby teams.GroupChartOrderNumber ascending
                                   select teams;

            List<String> teamsList = new List<string>();
            foreach (var team in teamsNameInOrder)
            {
                teamsList.Add(team.Name);
            }
            return teamsList.ToArray<String>();
        }

        public Boolean CheckIsEnabledForChartGroup(String _teamName)
        {
            var query = from team in this.Teams
                        where team.Name == _teamName && team.IsEnabledForGroupChart == true
                        select team;

            if (query.Count<Team>() == 0)
                return false;
            else
                return true;
        }

        public String[] getALLEnabledTeamsForChartGroupInOrder()
        {
            String[] allTeamNames = this.GetAllTeamNamesInOrder();

            if (allTeamNames == null)
                return null;

            List<String> enabledTeamNames = new List<string>();

            foreach (String team in allTeamNames)
            {
                if (this.CheckIsEnabledForChartGroup(team))
                {
                    enabledTeamNames.Add(team);
                }
            }

            return enabledTeamNames.ToArray<String>();
        }

        public void MoveNumberOfTeam(string _teamName, Boolean _moveDown)
        {
            
        }
        #endregion Public functionalities.
    }

    public class Team
    {
        public Team()
        {
            this.Members = new List<Member>();
            isEnabledForGroupChart = false;
            groupChartOrderNumber = 0;
        }

        #region private data
        private string g_name = "";
        private Boolean isEnabledForGroupChart;
        private int groupChartOrderNumber;
        private List<Member> g_member = null;
        #endregion

        [XmlAttribute(AttributeName = "Name")]
        public string Name
        {
            get { return g_name; }
            set { g_name = value; }
        }

        [XmlAttribute(AttributeName = "IsEnabledForGroupChart")]
        public Boolean IsEnabledForGroupChart
        {
            get { return isEnabledForGroupChart; }
            set { isEnabledForGroupChart = value; }
        }

        [XmlAttribute(AttributeName = "GroupChartOrderNumber")]
        public int GroupChartOrderNumber
        {
            get { return groupChartOrderNumber; }
            set { groupChartOrderNumber = value; }
        }

        [XmlElement(ElementName = "Member")]
        public List<Member> Members
        {
            get { return g_member; }
            set { g_member = value; }
        }

        public bool IsContainMember(string alias)
        {
            foreach (Member m in Members)
            {
                if (m.Alias == alias)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Member
    {
        #region private data
        private string m_alias = "";
        private string m_name = "";
        private Role m_role;
        private Boolean m_enable = true;
        #endregion

        [XmlAttribute(AttributeName = "MemberName")]
        public string MemberName
        {
            get { return m_name; }
            set { m_name = value; }
        }

        [XmlElement(ElementName = "Alias")]
        public string Alias
        {
            get { return m_alias; }
            set { m_alias = value; }
        }

        [XmlElement(ElementName = "Role")]
        public Role Role
        {
            get { return m_role; }
            set { m_role = value; }
        }

        [XmlElement(ElementName = "Enable")]
        public Boolean Enable
        {
            get { return m_enable; }
            set { m_enable = value; }
        }
    }

    public enum Role
    {
        Dev,
        Test,
        Other,
        PM
    }
}
