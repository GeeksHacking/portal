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

export interface GFieldControlProps {
  id?: string
  invalid?: boolean
  disabled?: boolean
  required?: boolean
}

/**
 * Resolves a control's id / invalid / disabled / required / aria-describedby from its own props,
 * falling back to the surrounding <GFormField>. Own props win; describedBy merges both.
 */
export function useGFieldControl(props: GFieldControlProps) {
  const field = useGFormField()
  const attrs = useAttrs()
  return {
    field,
    id: computed(() => props.id ?? field?.id.value),
    invalid: computed(() => !!props.invalid || !!field?.invalid.value),
    disabled: computed(() => !!props.disabled || !!field?.disabled.value),
    required: computed(() => !!props.required || !!field?.required.value),
    describedBy: computed(() =>
      [attrs['aria-describedby'] as string | undefined, field?.describedBy.value].filter(Boolean).join(' ') || undefined,
    ),
  }
}
