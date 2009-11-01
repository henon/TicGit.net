/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the TicGit project nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;

namespace TicGit.Net
{
    public class Ticket
    {
        public Base Base { get; private set; }
        public Hashtable Options { get; private set; }
        public string TicketId { get; set; }
        public string TicketName { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
        public DateTime Opened { get; set; }
        public string Assigned { get; set; }
        public string Milestone { get; set; }
        public string Email
        {
            get
            {
                return (Options["user_email"] as string) ?? "anon";
            }
        }
        public string User
        {
            get
            {
                return (Options["user_name"] as string) ?? "anon";
            }
        }
        public List<object> Attachments { get; private set; }
        public List<Comment> Comments { get; private set; }
        public List<string> Tags { get; private set; }
        private static Random m_rand = new Random(DateTime.Now.Millisecond);

        public Ticket(Base @base, Hashtable options)
        {
            Debug.Assert(@base != null);
            if (!options.ContainsKey("user_name"))
                options["user_name"] = @base.Git.Config["user.name"];
            if (!options.ContainsKey("user_email"))
                options["user_email"] = @base.Git.Config["user.email"];

            Base = @base;
            Options = options ?? new Hashtable();

            State = "open"; // by default
            Attachments = new List<object>();
            Comments = new List<Comment>();
            Tags = new List<string>();
        }

        public static Ticket Create(Base repo, string title, Hashtable options)
        {
            var t = new Ticket(repo, options);
            t.Title = title;
            t.TicketName = CreateTicketName(title);
            t.SaveNew();
            return t;
        }

        public static string CreateTicketName(string title)
        {
            //[Time.now.to_i.to_s, Ticket.clean_string(title), rand(999).to_i.to_s].join('_')
            var s = new StringBuilder();
            s.Append(GitSharp.Core.Util.DateTimeExtensions.ToUnixTime(DateTime.Now).ToString());
            s.Append("_");
            s.Append(Ticket.CleanString(title));
            s.Append("_");
            s.Append(m_rand.Next(999));
            return s.ToString();
        }


        //    public static Ticket Open(base, ticket_name, ticket_hash, options = {})
        public static Ticket Open(Base @base, string ticket_name, DirectoryInfo ticket_dir, Hashtable options)
        {
            string tid = null;
            var t = new Ticket(@base, options);
            t.TicketName = ticket_name;
            var h = ParseTicketName(ticket_name);
            if (h == null)
                return null;
            t.Title = h["title"] as string;
            t.Opened = (DateTime)h["time"];
            foreach (var f in ticket_dir.GetFiles())
            {
                if (f.Name == "TICKET_ID")
                    tid = File.ReadAllText(f.FullName);
                else
                {
                    var data = f.Name.Split('_');
                    if (data[0] == "ASSIGNED")
                        t.Assigned = data[1];
                    else if (data[0] == "COMMENT")
                        t.Comments.Add(new Comment(@base, f.FullName));
                    else if (data[0] == "TAG")
                        t.Tags.Add(data[1]);
                    else if (data[0] == "STATE")
                        t.State = data[1];
                }
            }
            t.TicketId = tid;
            return t;
        }

        public static Hashtable ParseTicketName(string name)
        {
            var tokens = name.Split('_');
            if (tokens.Length < 3)
                return null;
            var epoch = tokens[0];
            var title = tokens[1];
            title = Regex.Replace(title, "-", " ");
            var h = new Hashtable();
            h["title"] = title;
            h["time"] = GitSharp.Core.Util.DateTimeExtensions.UnixTimeToDateTime(Convert.ToInt64(epoch));
            return h;
        }

        // write this ticket to the git database
        public void SaveNew()
        {
            var dir = Base.CreateDirectory(TicketName);
            Base.AddFile(dir, "TICKET_ID", TicketName);
            Base.AddFile(dir, "ASSIGNED_" + CleanString( Assigned), Assigned);
            Debug.Assert(!string.IsNullOrEmpty(State));
            Base.AddFile(dir, "STATE_" + State, State);
            foreach (var comment in Comments)
                Base.AddFile(dir, CommentName(Email), comment.Text as string);
            var tags = Tags.Select(t => t.Trim()).ToArray();
            if (tags != null && tags.Length > 0)
            {
                foreach (var tag in tags)
                {
                    if (tag.Length == 0)
                        continue;
                    var tag_filename = "TAG_" + Ticket.CleanString(tag);
                    Base.AddFileIfNotExists(Path.Combine(TicketName, tag_filename), tag_filename);
                }
            }
            Base.Git.Commit("added ticket " + TicketName, new Author(User, Email));
        }

        private string CommentName(string Email)
        {
            var s = new StringBuilder();
            s.Append("COMMENT_");
            s.Append(GitSharp.Core.Util.DateTimeExtensions.ToUnixTime(DateTime.Now).ToString());
            s.Append("_");
            s.Append(Email);
            return s.ToString();
        }

        public static string CleanString(string @string)
        {
            return Regex.Replace(@string.ToLower(), "[^a-z0-9]+", "-", RegexOptions.IgnoreCase);
        }

        public void AddComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return;
            var comment_filename = Base.AddFile(Path.Combine(TicketName, CommentName(Email)), comment);
            Base.Git.Commit("added comment to ticket " + TicketName, new Author(User, Email));
            Comments.Add(new Comment(Base, comment_filename));
        }

        public void ChangeState(string new_state)
        {
            if (string.IsNullOrEmpty(new_state))
                return;
            if (new_state == State)
                return;
            Base.AddFile(Path.Combine(TicketName, "STATE_" + new_state), new_state);
            Base.RemoveFile(Path.Combine(TicketName, "STATE_" + State));
            Base.Git.Commit("added state (" + new_state + ") to ticket " + TicketName, new Author(User, Email));
            State = new_state;
        }

        public void ChangeAssigned(string new_assigned)
        {
            new_assigned = CleanString( new_assigned ?? ""); 
            if (new_assigned == Assigned)
                return;
            Base.AddFile(Path.Combine(TicketName, "ASSIGNED_" + new_assigned), new_assigned); 
            Base.RemoveFile(Path.Combine(TicketName, "ASSIGNED_" + Assigned));
            Base.Git.Commit("assigned " + new_assigned + " to ticket " + TicketName, new Author(User, Email));
            Assigned = new_assigned;
        }


        public void ChangeTags(string[] new_tags)
        {
            var new_tags_clean=new_tags.Select(t => CleanString(t.Trim())).ToArray();
            var remove_tags = new List<string>(Tags);
            var add_tags= new List<string>(new_tags_clean);
            // calculate difference  between old and new tagset
            foreach(var tag in new_tags_clean) 
                remove_tags.Remove(tag);
            foreach(var tag in Tags)
                add_tags.Remove(tag);
            if (add_tags.Count + remove_tags.Count == 0)
                return;
            // execute
            foreach (var tag in remove_tags)
                RemoveTagNoCommit(tag);
            foreach (var tag in add_tags)
                AddTagNoCommit(tag);
            Base.Git.Commit("changed tag(s) to (" + string.Join(", ", new_tags_clean) + ") of ticket " + TicketName, new Author(User, Email));
        }

        public void AddTag(params string[] tags)
        {
            foreach (var tag in tags)
                AddTagNoCommit(tag);
            Base.Git.Commit("added tag(s) (" + string.Join(", ", tags) + ") to ticket " + TicketName, new Author(User, Email));
        }

        private void AddTagNoCommit(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;
            var tag_filename = Path.Combine(TicketName, "TAG_" + tag);
            Base.AddFile(tag_filename, tag);
        }

        public void RemoveTag(params string[] tags)
        {
            foreach (var tag in tags)
                RemoveTagNoCommit(tag);
            Base.Git.Commit("removed tag(s) (" + string.Join(", ", tags) + ") from ticket " + TicketName, new Author(User, Email));
        }

        private void RemoveTagNoCommit(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return;
            var tag_filename = Path.Combine(TicketName, "TAG_" + tag);
            Base.RemoveFile(tag_filename);
        }

        public static void InDirectory(string directory, Action action)
        {
            var dir = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(directory);
                action();
            }
            finally
            {
                Directory.SetCurrentDirectory(dir);
            }
        }

        public static void Delete(Base b, Ticket t)
        {
            var path = Path.Combine(b.Git.WorkingDirectory, t.TicketName);
            b.Git.Index.Remove(path);
            b.Git.Commit("deleted " + t.TicketName, new Author(t.User, t.Email));
            DeleteDirectory(new DirectoryInfo(path));
        }

        private static void DeleteDirectory(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
                file.Delete();
            foreach (var subdir in dir.GetDirectories())
                DeleteDirectory(subdir);
            dir.Delete();
        }
    }
}
