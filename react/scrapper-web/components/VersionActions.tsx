import { ScrapperState, ScrapperStatus, ScrapperVersionMap, VersionAction } from '../features/projects/projectModels';

export interface VersionActionsProps {
  state?: ScrapperState;
  onAction: (action: VersionAction) => void;
}

export const VersionActions = ({ state, onAction }: VersionActionsProps) => {
  const renderParts = (status: ScrapperStatus) => {
    switch (status) {
      case 'continue':
        return <a onClick={() => onAction('pause')}>pause</a>;
      case 'pause':
      case 'failure':
        return <a onClick={() => onAction('resume')}>resume</a>;
    }
  };

  if (!state) {
    return <a onClick={() => onAction('start')}>start</a>;
  } else {
    return (
      <>
        {renderParts(state.status)} | <a onClick={() => onAction('reset')}>reset</a>
      </>
    );
  }
};
