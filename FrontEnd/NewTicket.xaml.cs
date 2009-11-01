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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections;
using System.Text.RegularExpressions;

namespace TicGit.Net.FrontEnd
{

    public partial class NewTicket : UserControl
    {
        public NewTicket()
        {
            InitializeComponent();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var h = new Hashtable();
            var title = m_title.Text;
            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Title must not be empty");
                return;
            }
            var t = new Ticket(App.Base, h)
            {
                TicketName = Ticket.CreateTicketName(title),
                Title = title,
                Opened = DateTime.Now,
                Assigned = m_assigned.Text,
                State=m_status.Text,
            };
            // add comment if any
            if (!string.IsNullOrEmpty(m_comment.Text))
                t.Comments.Add(new Comment(m_comment.Text));
            // add tags if any
            if (!string.IsNullOrEmpty(m_tags.Text))
                t.Tags.AddRange(m_tags.Text.Split(','));
            t.SaveNew();
            //if (TicketSaved != null)
                TicketSaved();
            //this.Close();
            //this.Owner.Focus();
        }

        public event Action TicketSaved;

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            //this.Close();
            TicketSaved();
        }

        private void AssignToMe(object sender, RoutedEventArgs e)
        {
            m_assigned.Text = App.Base.Git.Config["user.name"];
        }
    }
}
