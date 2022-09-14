import { ApiError, ApiResult } from '../sharedModels';

export type ScrapperStatus = 'continue' | 'pause' | 'finish' | 'schedule' | 'failure';

export interface ScrapperRequestBlock {
  from?: number;
  to?: number;
}

export interface ScrapperRequest {
  blockRange: ScrapperRequestBlock;
}

export interface ScrapperState {
  date: number;
  status: ScrapperStatus;
  request: ScrapperRequest;
}

export interface ScrapperVersion {
  id: string;
  createdAt: number;
  state?: ScrapperState;
}

export interface ScrapperVersionMap {
  [key: string]: ScrapperVersion;
}

export interface Project {
  id: string;
  address: string;
  name: string;
  abi: string;
  versions: ScrapperVersionMap;
  ethProviderUrl: string;
}

export type CreateProjectError = ApiError | { kind: 'get-abi-error' };

export type CreateProjectResult = ApiResult<Project, CreateProjectError>;

export interface ProjectState {
  [key: string]: Project;
}

export type VersionAction = 'start' | 'pause' | 'resume' | 'reset';
