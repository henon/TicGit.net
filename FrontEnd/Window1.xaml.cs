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
using TicGit.Net;
using Microsoft.Win32;

namespace TicGit.Net.FrontEnd
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            this.Loaded += (o, args) => Reload();
            m_list.SelectionChanged += (o, args) => UpdateTicketView();
            m_list.SelectionMode = SelectionMode.Single;
            m_repo.Text = App.TicketsPath;
            m_username.Text = App.Base.Git.Config["user.name"];
            m_email.Text = App.Base.Git.Config["user.email"];
        }

        private void UpdateTicketView()
        {
            m_comment.Text = "";
            var t = m_list.SelectedItem as Ticket;
            if (t == null)
            {
                m_title.Text = "(no ticket selected)";
                m_comments.ItemsSource = new Comment[0];
                m_assigned.Text = "";
                m_status.SelectedIndex = -1;
                m_tags.Text = "";
                return;
            }
            m_title.Text = t.Title;
            m_comments.ItemsSource = t.Comments;
            m_assigned.Text = t.Assigned;
            m_status.Text = t.State;
            m_tags.Text = string.Join(", ", t.Tags.ToArray());
        }

        private void OnNewTicket(object sender, RoutedEventArgs e)
        {
            var new_ticket_view = new NewTicket();
            m_new_ticket_container.Content = new_ticket_view;
            new_ticket_view.TicketSaved += () =>
            {
                m_new_ticket_container.Content = null;
                m_new_ticket_container.Visibility = Visibility.Collapsed;
                m_edit_ticket.Visibility = Visibility.Visible;
                Reload();
                m_list.SelectedIndex = 0;
            };
            m_edit_ticket.Visibility = Visibility.Collapsed;
            m_new_ticket_container.Visibility = Visibility.Visible;
            ////m_sidebar.Content = new_ticket_view;
            //new_ticket_view.Width = 400;
            //new_ticket_view.Height = 600;
            //new_ticket_view.Owner = this;
            //new_ticket_view.Show();
        }

        public void Reload()
        {
            var i = m_list.SelectedIndex;
            var state_filter = new HashSet<string>(new[] { m_open, m_resolved, m_invalid, m_hold }.Where(ui => ui.IsChecked == true).Select(ui => ui.Content as string));
            var tics = App.Base.Tickets.Reverse().ToArray();
            m_list.ItemsSource = tics.Where(t => state_filter.Contains(t.State)).ToArray();
            m_list.SelectedIndex = i;
            if (m_list.SelectedIndex == -1)
                m_list.SelectedIndex = 0;
            m_ticno.Text = "(" + m_list.Items.Count + ")";
            m_tic_total.Text = "(" + tics.Length + ")";
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            var t = m_list.SelectedItem as Ticket;
            if (t == null)
                return;
            t.AddComment(m_comment.Text);
            t.ChangeState(m_status.Text);
            t.ChangeAssigned(m_assigned.Text);
            t.ChangeTags(m_tags.Text.Split(','));
            var i = m_list.SelectedIndex;
            Reload(); // todo, reload this item only instead of reloading the complete list.
            m_list.SelectedIndex = i;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            UpdateTicketView();
        }

        private void AssignToMe(object sender, RoutedEventArgs e)
        {
            m_assigned.Text = App.Base.Git.Config["user.name"];
        }

        private void OnReload(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void OnMarkResolved(object sender, RoutedEventArgs e)
        {
            m_status.SelectedIndex = 1;
        }

        private void OnDeleteTicket(object sender, RoutedEventArgs e)
        {
            var t = (sender as MenuItem).Tag as Ticket;
            Ticket.Delete(App.Base, t);
            Reload();
        }

        private void OnOpenRepository(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select a ticgit directory or a new directory to initialize one:",
                ShowNewFolderButton = true,
            };
            if (dlg.ShowDialog() ==  System.Windows.Forms.DialogResult.OK)
                App.TicketsPath = dlg.SelectedPath;
            Reload();
        }

        private void OnShowCopyright(object sender, RoutedEventArgs e)
        {
            new CopyrightView().ShowDialog();
        }
    }

    public class CommentCountConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var t = value as Ticket;
            if (t == null)
                return "(0)";
            return "(" + t.Comments.Count + ")";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
