import { createAction, createSlice, PayloadAction } from '@reduxjs/toolkit';
import { omit, set } from 'lodash';
import { call, put, takeEvery } from 'redux-saga/effects';
import { RootState } from '../../app/store';
import { Project, ProjectState, ScrapperState, VersionAction } from './projectModels';
import { getProjects } from './projectsService';

export interface AddActionPayload {
  id: string;
  name: string;
}

export interface VersionActionPayload {
  projectId: string;
  versionId: string;
  action: VersionAction;
  state: ScrapperState;
}

const initialState: ProjectState = {};

// refresh
// set rehydr config

export const projectsSlice = createSlice({
  name: 'projects',
  initialState,
  reducers: {
    fetchAllSuccess: (state, action: PayloadAction<ProjectState>) => {
      return action.payload;
    },
    add: (state, action: PayloadAction<Project>) => {
      return { [action.payload.id]: action.payload, ...state };
    },
    remove: (state, action: PayloadAction<string>) => {
      return omit(state, action.payload);
    },
    setVersionState: (state, action: PayloadAction<VersionActionPayload>) => {
      return set(
        state,
        [action.payload.projectId, 'versions', action.payload.versionId, 'state'],
        action.payload.state,
      );
    },
    stateChanges: (state, action: PayloadAction<ProjectState>) => {
      return action.payload;
    },
  },
});

export const selectProjects = (state: RootState) => state.projects;

export const { add, remove, setVersionState } = projectsSlice.actions;

export default projectsSlice.reducer;

//
export const fetchAllRequest = createAction('projects/fetchAllRequest');

async function fetchProjects() {
  const projects = await getProjects();
  if (projects.kind === 'ok') {
    const data = Object.fromEntries(projects.value.map((x) => [x.id, x]));
    return data;
  } else {
    return null;
  }
}

function* fetchAll() {
  const projects: ProjectState = yield call(fetchProjects);
  yield put(projectsSlice.actions.fetchAllSuccess(projects));
}

//
export const fetchStateChangesRequest = createAction('projects/fetchStateChangesRequest');

function* fetchStateChanges() {
  const projects: ProjectState = yield call(fetchProjects);
  yield put(projectsSlice.actions.stateChanges(projects));
}

//
export function* projectsSaga() {
  yield takeEvery(fetchAllRequest.toString(), fetchAll);
  yield takeEvery(fetchStateChangesRequest.toString(), fetchStateChanges);
}
