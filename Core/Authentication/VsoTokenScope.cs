using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public class VsoTokenScope
    {
        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and 
        /// requests, and the ability to receive notifications about build events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope BuildAccess = new VsoTokenScope("vso.build");
        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and 
        /// requests, and the ability to queue a build, update build properties, and the ability to 
        /// receive notifications about build events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope BuildExecute = new VsoTokenScope("vso.build_execute");
        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the 
        /// ability to manage rooms and users and to receive notifications about new messages via 
        /// service hooks.
        /// </summary>
        public static readonly VsoTokenScope ChatManage = new VsoTokenScope("vso.chat_manage");
        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the 
        /// ability to receive notifications about new messages via service hooks.
        /// </summary>
        public static readonly VsoTokenScope ChatWrite = new VsoTokenScope("vso.chat_write");
        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about 
        /// commits, changesets, branches, and other version control artifacts. Also grants the 
        /// ability to create and manage code repositories, create and manage pull requests and 
        /// code reviews, and to receive notifications about version control events via service 
        /// hooks.
        /// </summary>
        public static readonly VsoTokenScope CodeManage = new VsoTokenScope("vso.code_manage");
        /// <summary>
        /// Grants the ability to read source code and metadata about commits, changesets, branches, 
        /// and other version control artifacts. Also grants the ability to get notified about 
        /// version control events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope CodeRead = new VsoTokenScope("vso.code");
        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about 
        /// commits, changesets, branches, and other version control artifacts. Also grants the 
        /// ability to create and manage pull requests and code reviews and to receive 
        /// notifications about version control events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope CodeWrite = new VsoTokenScope("vso.code_write");
        /// <summary>
        /// Grants the ability to read, write, and delete feeds and packages.
        /// </summary>
        public static readonly VsoTokenScope PackagingManage = new VsoTokenScope("vso.packaging_manage");
        /// <summary>
        /// Grants the ability to list feeds and read packages in those feeds.
        /// </summary>
        public static readonly VsoTokenScope PackagingRead = new VsoTokenScope("vso.packaging");
        /// <summary>
        /// Grants the ability to list feeds and read, write, and delete packages in those feeds.
        /// </summary>
        public static readonly VsoTokenScope PackagingWrite = new VsoTokenScope("vso.packaging_write");
        /// <summary>
        /// Grants the ability to read your profile, accounts, collections, projects, teams, and 
        /// other top-level organizational artifacts.
        /// </summary>
        public static readonly VsoTokenScope ProfileRead = new VsoTokenScope("vso.profile");
        /// <summary>
        /// Grants the ability to read service hook subscriptions and metadata, including supported
        /// events, consumers, and actions.
        /// </summary>
        public static readonly VsoTokenScope ServiceHookRead = new VsoTokenScope("vso.hooks");
        /// <summary>
        /// Grants the ability to create and update service hook subscriptions and read metadata, 
        /// including supported events, consumers, and actions."
        /// </summary>
        public static readonly VsoTokenScope ServiceHookWrite = new VsoTokenScope("vso.hooks_write");
        /// <summary>
        /// Grants the ability to read test plans, cases, results and other test management related 
        /// artifacts.
        /// </summary>
        public static readonly VsoTokenScope TestRead = new VsoTokenScope("vso.test");
        /// <summary>
        /// Grants the ability to read, create, and update test plans, cases, results and other 
        /// test management related artifacts.
        /// </summary>
        public static readonly VsoTokenScope TestWrite = new VsoTokenScope("vso.test_write");
        /// <summary>
        /// Grants the ability to read work items, queries, boards, area and iterations paths, and 
        /// other work item tracking related metadata. Also grants the ability to execute queries 
        /// and to receive notifications about work item events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope WorkRead = new VsoTokenScope("vso.work");
        /// <summary>
        /// Grants the ability to read, create, and update work items and queries, update board 
        /// metadata, read area and iterations paths other work item tracking related metadata, 
        /// execute queries, and to receive notifications about work item events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope WorkWrite = new VsoTokenScope("vso.work_write");

        private VsoTokenScope(string value)
        {
            Value = value;
        }

        public readonly string Value;

        public override String ToString()
        {
            return Value;
        }

        public static VsoTokenScope operator |(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            return new VsoTokenScope(scope1.Value + " " + scope2.Value);
        }
    }
}
