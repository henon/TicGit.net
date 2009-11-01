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
using System.IO;

namespace TicGit.Net
{
    public class RepositoryNotFoundException : IOException { }

    public class Base
    {

        public Repository Git { get; private set; }
        // Logger
        public string TicWorking { get; private set; }
        public string TickIndex { get; private set; }

        public IEnumerable<Ticket> Tickets
        {
            get
            {
                foreach (var dir in new DirectoryInfo(Git.WorkingDirectory).GetDirectories())
                {
                    if (dir.Name == ".git")
                        continue;
                    var t=Ticket.Open(this, dir.Name, dir, new Hashtable());
                    if (t != null)
                        yield return t;
                }
            }
        }

        public IEnumerable<Ticket> LastTickets
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Ticket CurrentTicket { get; private set; }

        public Hashtable Config { get; private set; }
        public string State { get; private set; }
        //ConfigFile
        //rivate string _tic_dir;

        public Base(string git_dir) // , Hashtable options
        {
            if (!new DirectoryInfo(git_dir).Exists)
            {
                Directory.CreateDirectory(git_dir);
                Git = Repository.Init(git_dir);
                AddFile(".hold", "hold");
                Git.Commit("initialized ticket repository", new Author(Git.Config["user.name"], Git.Config["user.email"]));
            }
            else
            Git = new Repository(git_dir);
            //@logger = opts[:logger] || Logger.new(STDOUT)
            var proj = Ticket.CleanString(Git.Directory);

            //@tic_dir = opts[:tic_dir] || '~/.ticgit'
            // @tic_working = opts[:working_directory] || File.expand_path(File.join(@tic_dir, proj, 'working'))
            // @tic_index = opts[:index_file] || File.expand_path(File.join(@tic_dir, proj, 'index'))

            // # load config file
            // @config_file = File.expand_path(File.join(@tic_dir, proj, 'config.yml'))
            // if File.exists?(config_file)
            //   @config = YAML.load(File.read(config_file))
            // else
            //   @config = {}
            // end

            // @state = File.expand_path(File.join(@tic_dir, proj, 'state'))

            // if File.exists?(@state)
            //   load_state
            // else
            //   reset_ticgit
            // end
        }

        public string FindRepo(string dir)
        {
            var full = System.IO.Path.GetFullPath(dir);
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GIT_WORKING_DIR")))
            {
                while (true)
                {
                    if (new DirectoryInfo(Path.Combine(full, ".git")).Exists)
                        return full;
                    full = Path.GetDirectoryName(full);
                    if (Path.GetPathRoot(full) == full)
                        throw new RepositoryNotFoundException();
                }
            }
            return Directory.GetCurrentDirectory();
        }

        internal string CreateDirectory(string directory)
        {
            var dir = Path.Combine(Git.WorkingDirectory, directory);
            Directory.CreateDirectory(dir);
            return dir;
        }

        internal string CreateFile(string filename, string content)
        {
            string file = filename;
            if (Path.GetFullPath(file) != file) // if not absolute, then absolutize
                file = Path.Combine(Git.WorkingDirectory, file); 
            File.WriteAllText(file, content);
            return file;
        }

        internal string AddFile(string dir, string filename, string content)
        {
            var path = Path.Combine(dir, filename);
            CreateFile(path, content);
            Git.Index.Add(path);
            return path;
        }

        internal string AddFileIfNotExists(string filename, string content)
        {
            string file = filename;
            if (Path.GetFullPath(file) != file) // if not absolute, then absolutize
                file = Path.Combine(Git.WorkingDirectory, file);
            if (new FileInfo(file).Exists)
                return null;
            File.WriteAllText(file, content);
            Git.Index.Add(file);
            return file;
        }

        internal string AddFile(string filename, string comment)
        {
            return AddFile(Git.WorkingDirectory, filename, comment);
        }

        internal void RemoveFile(string filename)
        {
            string file = filename;
            if (Path.GetFullPath(file) != file) // if not absolute, then absolutize
                file = Path.Combine(Git.WorkingDirectory, file);
            if (!new FileInfo(file).Exists)
                return;
            Git.Index.Remove(file);
            new FileInfo(file).Delete();
        }
    }
}
