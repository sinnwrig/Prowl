// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime;
using Prowl.Runtime.Utils;

namespace Prowl.Editor
{
    [FilePath("Projects.cache", FilePathAttribute.Location.EditorPreference)]
    public class ProjectCache : ScriptableSingleton<ProjectCache>, ISerializationCallbackReceiver
    {
        [NonSerialized]
        private List<Project> _projectCache = [];

        [SerializeField]
        private List<string> _serializedProjects = [];


        public int ProjectsCount
        {
            get
            {
                _projectCache ??= new();
                return _projectCache.Count;
            }
        }


        public void AddProject(Project project)
        {
            _projectCache.Add(project);
            Save();
        }

        public Project? GetProject(int index)
        {
            Project project = _projectCache[index];
            project.Refresh();

            if (!project.Exists)
            {
                _projectCache.RemoveAt(index);
                Save();
                return null;
            }

            return project;
        }

        public void RemoveProject(Project project)
        {
            _projectCache.RemoveAll(x => x.ProjectPath == project.ProjectPath);
            Save();
        }


        public void OnBeforeSerialize()
        {
            _serializedProjects = _projectCache.Select(x => x.ProjectPath).ToList();
        }


        public void OnAfterDeserialize()
        {
            _serializedProjects ??= [];
            _projectCache = [];

            foreach (string path in _serializedProjects)
            {
                DirectoryInfo info = new DirectoryInfo(path);

                if (info.Exists)
                    _projectCache.Add(new Project(info));
            }
        }
    }
}
