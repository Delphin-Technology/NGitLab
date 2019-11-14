﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGitLab.Models;

namespace NGitLab.Mock.Clients
{
    internal sealed class ProjectClient : ClientBase, IProjectClient
    {
        public ProjectClient(ClientContext context)
            : base(context)
        {
        }

        public Models.Project this[int id]
        {
            get
            {
                var project = GetProject(id, ProjectPermission.View);
                if (project == null || !project.CanUserViewProject(Context.User))
                    throw new GitLabNotFoundException();

                return project.ToClientProject();
            }
        }

        public Models.Project this[string fullName]
        {
            get
            {
                var project = GetProject(fullName, ProjectPermission.View);
                return project.ToClientProject();
            }
        }

        public IEnumerable<Models.Project> Accessible => Server.AllProjects.Where(project => project.IsUserMember(Context.User)).Select(project => project.ToClientProject());

        public IEnumerable<Models.Project> Owned => Server.AllProjects.Where(project => project.IsUserOwner(Context.User)).Select(project => project.ToClientProject());

        public IEnumerable<Models.Project> Visible => Server.AllProjects.Where(project => project.CanUserViewProject(Context.User)).Select(project => project.ToClientProject());

        public Models.Project Create(ProjectCreate project)
        {
            var parentGroup = GetParentGroup(project.NamespaceId);

            var newProject = new Project(project.Name)
            {
                Description = project.Description,
                Visibility = project.VisibilityLevel,
                Permissions =
                {
                    new Permission(Context.User, AccessLevel.Owner),
                }
            };

            parentGroup.Projects.Add(newProject);
            return newProject.ToClientProject();
        }

        private Group GetParentGroup(string namespaceId)
        {
            Group parentGroup;
            if (namespaceId != null)
            {
                parentGroup = Server.AllGroups.FindGroup(namespaceId);
                if (parentGroup == null || !parentGroup.CanUserViewGroup(Context.User))
                    throw new GitLabNotFoundException();

                if (!parentGroup.CanUserAddProject(Context.User))
                    throw new GitLabForbiddenException();
            }
            else
            {
                parentGroup = Context.User.Namespace;
            }

            return parentGroup;
        }

        public void Delete(int id)
        {
            var project = GetProject(id, ProjectPermission.Delete);
            project.Remove();
        }

        public Models.Project Fork(string id, ForkProject forkProject)
        {
            EnsureUserIsAuthenticated();

            var project = GetProject(id, ProjectPermission.View);
            var group = forkProject.Namespace != null ? GetParentGroup(forkProject.Namespace) : Context.User.Namespace;
            var newProject = new Project(forkProject.Name ?? project.Name)
            {
                Description = project.Description,
                ForkedFrom = project,
                ImportStatus = "finished",
            };

            newProject.Permissions.Add(new Permission(Context.User, AccessLevel.Owner));

            group.Projects.Add(newProject);

            return newProject.ToClientProject();
        }

        public IEnumerable<Models.Project> Get(ProjectQuery query)
        {
            throw new NotImplementedException();
        }

        public Models.Project GetById(int id, SingleProjectQuery query)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Models.Project> GetForks(string id, ForkedProjectQuery query)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, double> GetLanguages(string id)
        {
            throw new NotImplementedException();
        }

        public Models.Project Update(string id, ProjectUpdate projectUpdate)
        {
            var project = GetProject(id, ProjectPermission.Edit);
            project.Name = projectUpdate.Name;
            project.DefaultBranch = projectUpdate.DefaultBranch;
            project.Description = projectUpdate.Description;

            if (projectUpdate.Visibility.HasValue)
            {
                project.Visibility = projectUpdate.Visibility.Value;
            }

            return project.ToClientProject();
        }
    }
}
