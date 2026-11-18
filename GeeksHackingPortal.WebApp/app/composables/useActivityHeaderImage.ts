import {
  useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageUploadEndpoint,
  useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageUploadUrlEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref } from 'vue'

/** Mirrors the API limits on POST /admin/activities/{id}/header-image/upload-url. */
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
 *
 * Uploads go straight from the browser to the storage bucket: the API hands out a presigned
 * PUT URL, the file is sent there, and the API is then told which object to use.
 */
export function useActivityHeaderImage() {
  const uploadUrlMutation = useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageUploadUrlEndpoint()
  const uploadMutation = useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageUploadEndpoint()
  const deleteMutation = useGeeksHackingPortalApiEndpointsAdminActivitiesHeaderImageDeleteEndpoint()
  const isUploadingToStorage = ref(false)

  async function uploadFile(activityId: string, file: File) {
    const { uploadUrl, key } = await uploadUrlMutation.mutateAsync({
      path: { activityId },
      body: { contentType: file.type, contentLength: file.size },
    })
    if (!uploadUrl || !key)
      throw new Error('The API did not return an upload URL.')

    isUploadingToStorage.value = true
    try {
      // Content-Type is part of the signature, so it must match what was requested.
      const response = await fetch(uploadUrl, {
        method: 'PUT',
        body: file,
        headers: { 'Content-Type': file.type },
      })
      if (!response.ok)
        throw new Error(`Image upload failed with status ${response.status}.`)
    }
    finally {
      isUploadingToStorage.value = false
    }

    await uploadMutation.mutateAsync({ path: { activityId }, body: { key } })
  }

  /** Applies the change. Returns the error if it failed, or null on success / nothing to do. */
  async function saveHeaderImage(activityId: string | null | undefined, change: HeaderImageChange): Promise<unknown> {
    if (!activityId || (!change.file && !change.removed))
      return null

    try {
      if (change.file)
        await uploadFile(activityId, change.file)
      else
        await deleteMutation.mutateAsync({ path: { activityId } })
      return null
    }
    catch (error) {
      console.error('Failed to save header image', error)
      return error
    }
  }

  const isSavingHeaderImage = computed(() =>
    uploadUrlMutation.isPending.value
    || isUploadingToStorage.value
    || uploadMutation.isPending.value
    || deleteMutation.isPending.value,
  )

  return { saveHeaderImage, isSavingHeaderImage }
}
