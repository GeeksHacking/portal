import type { ComputedRef, InjectionKey } from 'vue'

/**
 * Shared state between <GFormField> and the control inside it (e.g. <GInput>),
 * so the label, hint and error are wired to the control without passing ids by hand.
 */
export interface GFormFieldContext {
  id: ComputedRef<string>
  describedBy: ComputedRef<string | undefined>
  invalid: ComputedRef<boolean>
  required: ComputedRef<boolean>
  disabled: ComputedRef<boolean>
}

export const G_FORM_FIELD_KEY: InjectionKey<GFormFieldContext> = Symbol('GFormField')

export function provideGFormField(context: GFormFieldContext) {
  provide(G_FORM_FIELD_KEY, context)
}

/** Returns the surrounding field's context, or undefined when the control is used on its own. */
export function useGFormField() {
  return inject(G_FORM_FIELD_KEY, undefined)
}
