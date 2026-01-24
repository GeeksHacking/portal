<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const route = useRoute()
const config = useRuntimeConfig()
const hackathonId = route.params.hackathonId as string

const { data: user, isLoading: authLoading } = useQuery(authQueries.whoAmI)
const { data: status, isLoading: statusLoading } = useQuery(hackathonQueries.status(hackathonId))

watch(
  [() => user.value, authLoading, () => status.value, statusLoading],
  ([userData, authIsLoading, statusData, statusIsLoading]) => {
    if (authIsLoading) return
    if (!userData) {
      navigateTo(`${config.public.api}/auth/login`, { external: true })
      return
    }
    if (statusIsLoading) return
    if (!statusData?.isParticipant) {
      navigateTo(`/${hackathonId}/registration`, { replace: true })
    } else {
      navigateTo(`/${hackathonId}/team`, { replace: true })
    }
  },
  { immediate: true },
)
</script>
