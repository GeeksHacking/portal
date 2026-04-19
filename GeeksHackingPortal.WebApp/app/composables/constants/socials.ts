// in the future if social links can be fetched from API, we can do it here

export interface SocialLink {
  name: string
  url: string
  icon: string
}

export const SOCIAL_LINKS: SocialLink[] = [
  {
    name: 'Telegram',
    url: 'https://t.me/joinchat/B5fU10aB8z_6tAf1YV8q7Q',
    icon: 'fa6-brands:telegram',
  },
  {
    name: 'Instagram',
    url: 'https://www.instagram.com/geekshacking/',
    icon: 'fa6-brands:instagram',
  },
  {
    name: 'Facebook',
    url: 'https://www.facebook.com/groups/geekshacking/',
    icon: 'fa6-brands:facebook',
  },
  {
    name: 'LinkedIn',
    url: 'https://www.linkedin.com/company/geekshacking',
    icon: 'fa6-brands:linkedin',
  },
]

export const CONTACT_EMAIL = 'contact@geekshacking.com'
export const SPONSOR_EMAIL = 'sponsor@geekshacking.com'
