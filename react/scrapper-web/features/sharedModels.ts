export interface Ok<T> {
  kind: 'ok';
  value: T;
}

export interface Error<T> {
  kind: 'error';
  error: T;
}

export type Result<S, E> = Ok<S> | Error<E>;

export interface ApiResponseError {
  kind: 'api-response-error';
  status: number;
  statusText: string;
}

export interface ApiNetworkError {
  kind: 'api-network-error';
}

export type ApiError = ApiResponseError | ApiNetworkError;

export type ApiResult<S, E = ApiError> = Result<S, E>;
