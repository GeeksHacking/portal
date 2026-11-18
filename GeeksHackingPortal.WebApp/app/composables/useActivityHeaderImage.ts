import {
  useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageUploadEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed } from 'vue'

/** Mirrors the API limits on PUT /admin/activities/{id}/header-image. */
export const HEADER_IMAGE_ACCEPT = ['image/jpeg', 'image/png', 'image/webp']
export const HEADER_IMAGE_MAX_BYTES = 5 * 1024 * 1024

/** What the admin chose in the header image field: a new file, a removal, or nothing. */
export interface HeaderImageChange {
  file: File | null
  removed: boolean
}

export function emptyHeaderImageChange(): HeaderImageChange {
  return { file: null, removed: false }
}

/**
 * Uploads or removes an activity's header image (admin only). Called after the activity
 * itself has been created or updated, because the upload needs the activity's id.
 */
export function useActivityHeaderImage() {
  const uploadMutation = useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageUploadEndpoint()
  const deleteMutation = useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageDeleteEndpoint()

  /** Applies the change. Returns the error if it failed, or null on success / nothing to do. */
  async function saveHeaderImage(activityId: string | null | undefined, change: HeaderImageChange): Promise<unknown> {
    if (!activityId || (!change.file && !change.removed))
      return null

    try {
      if (change.file)
        await uploadMutation.mutateAsync({ path: { activityId }, body: { file: change.file } })
      else
        await deleteMutation.mutateAsync({ path: { activityId } })
      return null
    }
    catch (error) {
      console.error('Failed to save header image', error)
      return error
    }
  }

  const isSavingHeaderImage = computed(() => uploadMutation.isPending.value || deleteMutation.isPending.value)

  return { saveHeaderImage, isSavingHeaderImage }
}
