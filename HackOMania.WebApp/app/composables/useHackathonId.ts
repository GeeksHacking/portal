export const useHackathonId = () => {
  return useCookie<string>('hackathonId', {
    maxAge: 60 * 60 * 24 * 30, // 30 days
    sameSite: 'lax',
  })
}
