// $Id: JoinRsp.java,v 1.2 2004/03/30 06:47:18 belaban Exp $


using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;



namespace Alachisoft.NGroups.Protocols.pbcast
{
    internal class JoinRsp : ICompactSerializable
    {
        public View View { get { return view; } }
        public Digest Digest { get { return digest; } }
        public JoinResult JoinResult { get { return joinResult; } set { joinResult = value; } }

        private View view = null;
        private Digest digest = null;
        private JoinResult joinResult = JoinResult.Success;

        public JoinRsp(View v, Digest d)
        {
            view = v;
            digest = d;
        }

        public JoinRsp(View v, Digest d, JoinResult result)
        {
            view = v;
            digest = d;
            joinResult = result;
        }
        
        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("view: ");
            if (view == null)
                sb.Append("<null>");
            else
                sb.Append(view);
            sb.Append(", digest: ");
            if (digest == null)
                sb.Append("<null>");
            else
                sb.Append(digest);
            sb.Append(", join result: ");
            switch (joinResult)
            {
                case JoinResult.Success:
                    sb.Append("success");
                    break;
                case JoinResult.MaxMbrLimitReached:
                    sb.Append("more than 2 nodes can not join the cluster");
                    break;
                case JoinResult.HandleJoinInProgress:
                    sb.Append("Handle Join called");
                    break;
                case JoinResult.HandleLeaveInProgress:
                    sb.Append("Handle Join called");
                    break;
                case JoinResult.MembershipChangeAlreadyInProgress:
                    sb.Append("Membership Change Already In Progress");
                    break;

            }
            return sb.ToString();
        }

        #region ICompactSerializable Members

        public void Deserialize(CompactReader reader)
        {
            view = reader.ReadObject() as View;
            digest = reader.ReadObject() as Digest;
            joinResult = (JoinResult)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(view);
            writer.WriteObject(digest);
            writer.WriteObject(joinResult);
        }

        #endregion
    }
}
