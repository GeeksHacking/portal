import { useMutation as useVueQueryMutation } from '@tanstack/vue-query'
import type {
  DefaultError,
  DistributiveOmit,
  MutationObserverOptions,
  MutationObserverResult,
  QueryClient,
  UseMutationOptions,
  UseMutationReturnType,
} from '@tanstack/vue-query'

export type { MutationObserverOptions, QueryClient, UseMutationReturnType }

/**
 * Public stand-in for Vue Query's unexported `MutationResult`.
 * Passing it as `TResult` keeps declaration emit from naming the private type (TS2883).
 */
export type MutationResult<TData, TError, TVariables, TOnMutateResult> =
  DistributiveOmit<
    MutationObserverResult<TData, TError, TVariables, TOnMutateResult>,
    'mutate' | 'reset'
  >

/**
 * `useMutation` wrapper with a fully nameable return type for TypeScript 7 `.d.ts` emit.
 */
export function useMutation<
  TData = unknown,
  TError = DefaultError,
  TVariables = void,
  TOnMutateResult = unknown,
>(
  options: UseMutationOptions<TData, TError, TVariables, TOnMutateResult>,
  queryClient?: QueryClient,
): UseMutationReturnType<
  TData,
  TError,
  TVariables,
  TOnMutateResult,
  MutationResult<TData, TError, TVariables, TOnMutateResult>
> {
  return useVueQueryMutation(options, queryClient)
}
