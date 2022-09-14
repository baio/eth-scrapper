import { createAction, createSlice, PayloadAction } from '@reduxjs/toolkit';
import { set } from 'lodash';
import { eventChannel } from 'redux-saga';
import { call, put, select, takeEvery } from 'redux-saga/effects';
import { RootState } from '../../app/store';
import appConfig from '../../config';
import { Project } from '../projects/projectModels';
import projectsSlice, { fetchStateChangesRequest } from '../projects/projectsSlice';
import { ApiResult, Result } from '../sharedModels';
import { AppConfig, AppState } from './appModels';
import { getEthBlockNumber, getEthProviderUrl } from './appService';

const initialState: AppState = { ethBlockNumber: 0, config: { ethProviderUrl: null } };

export const appSlice = createSlice({
  name: 'app',
  initialState,
  reducers: {
    setEthProviderUrl: (state, action: PayloadAction<string>) => {
      return set(state, ['config', 'ethProviderUrl'], action.payload);
    },
    setEthBlockNumber: (state, action: PayloadAction<number>) => {
      return { ...state, ethBlockNumber: action.payload };
    },
    setConfig: (state, action: PayloadAction<AppConfig>) => {
      return { ...state, config: action.payload };
    },
  },
});

export const selectApp = (state: RootState) => state.app;

export default appSlice.reducer;

export const { setEthProviderUrl, setEthBlockNumber } = appSlice.actions;

export const rehydrateConfigRequest = createAction('app/rehydrateConfigRequest');

function* rehydrateConfig() {
  const ethProviderUrl: string = yield call(getEthProviderUrl);
  if (ethProviderUrl !== null) {
    yield put(appSlice.actions.setConfig({ ethProviderUrl }));
    yield put(blockNumberQuery());
  }
}

//
export const blockNumberQuery = createAction('projects/blockNumberQuery');

function* fetchBlockNumber() {
  //@ts-ignore
  const { app }: RootState = yield select();
  const ethProviderUrl = app.config.ethProviderUrl;
  if (!!ethProviderUrl) {
    const ethBlockNumber: Result<number, string> = yield call(getEthBlockNumber, ethProviderUrl);
    if (ethBlockNumber.kind === 'ok') {
      yield put(setEthBlockNumber(ethBlockNumber.value));
    }
  }
}

//
function queryStateChangesChannel(interval: number) {
  return eventChannel((emitter) => {
    const iv = setInterval(() => {
      emitter(true);
    }, interval);
    // The subscriber must return an unsubscribe function
    return () => {
      clearInterval(iv);
    };
  });
}

//
export const stateChangesQuery = createAction('projects/stateChangesQuery');

export function* queryStateChanges() {
  yield put(blockNumberQuery());
  yield put(fetchStateChangesRequest());
}

//
export function* appSaga() {
  yield takeEvery(rehydrateConfigRequest.toString(), rehydrateConfig);
  yield takeEvery(blockNumberQuery.toString(), fetchBlockNumber);
  yield takeEvery(stateChangesQuery.toString(), queryStateChanges);
  //@ts-ignore
  // const stateChangesChannel = yield call(queryStateChangesChannel, appConfig.stateChangesQueryInterval);
  // yield takeEvery(stateChangesChannel, queryStateChanges);
}
