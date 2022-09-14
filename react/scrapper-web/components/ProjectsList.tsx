import { ProjectState, VersionAction } from '../features/projects/projectModels';
import { VersionsList } from './VersionsList';

export interface ProjectsListProps {
  projects: ProjectState;
  onRemove: (id: string) => void;
  onVersionAction: (projectId: string, versionId: string, action: VersionAction) => void;
  onRefresh: () => void;
  ethBlockNumber: number;
}

const ProjectsList = ({ projects, onRemove, onVersionAction, onRefresh, ethBlockNumber }: ProjectsListProps) => {
  return (
    <>
      <a style={{paddingTop: '15px', display: 'block'}} onClick={onRefresh}>refresh</a>
      <ul>
        {Object.values(projects).map((project) => (
          <li key={project.id}>
            {project.name} <a onClick={() => onRemove(project.id)}>remove</a>
            <VersionsList
              onAction={(versionId, action) => onVersionAction(project.id, versionId, action)}
              versions={project.versions}
              ethBlockNumber={ethBlockNumber}></VersionsList>
          </li>
        ))}
      </ul>
    </>
  );
};

export default ProjectsList;
