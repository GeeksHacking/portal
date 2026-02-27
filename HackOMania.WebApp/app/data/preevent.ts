export interface ItineraryItem {
  time: string
  event: string
  bgColor?: string
}

export interface WorkshopItem {
  logo: string
  logoClass?: string
  title: string
  description: string
  speakerName: string
  speakerBackground: string
}

export const itineraryItems: ItineraryItem[] = [
  { time: '9:30 AM', event: 'Registration' },
  { time: '10:00 AM', event: 'Welcome Introduction' },
  { time: '10:45 AM', event: 'Sharing by NETS' },
  { time: '11:15 AM', event: 'Release of Challenge Statements' },
  { time: '12:25 PM', event: 'Workshop 1: Interledger', bgColor: 'bg-[linear-gradient(to_right,#FFD0D000_0%,#FFC8CF12_14%,#FFDBBA24_28%,#FFCFAC36_42%,#BDD79E48_56%,#80D3BD5A_70%,#9AD2C56B_84%,#A2BDDB80_100%)]' },
  { time: '1:25 PM', event: 'Lunch' },
  { time: '1:55 PM', event: 'Workshop 2: ClickHouse', bgColor: 'bg-[linear-gradient(to_right,#FFA94600_0%,#FBFF4680_100%)]' },
  { time: '2:55 PM', event: 'Break' },
  { time: '3:05 PM', event: 'Workshop 3: GeeksHacking', bgColor: 'bg-[linear-gradient(to_right,#FFF40200_0%,#FFF40280_100%)]' },
  { time: '3:50 PM', event: 'Closing, Lucky Draw & Phototaking' },
  { time: '4:20 PM', event: 'Networking' },
]

export const workshopItems: WorkshopItem[] = [
  {
    logo: '/logos/Interledger Foundation (3).png',
    title: 'Open Payments',
    description: 'This tutorial introduces Open Payments, an open standard that enables third-party applications, like e-commerce stores, to gain direct, secure access to user accounts. These accounts can exist on any financial platform, from mobile money to bank accounts to digital wallets, in any currency, anywhere in the world. Learn how Open Payments simplifies transactions, enhances transparency and user control, protects sensitive financial information, and empowers developers to integrate payments with just a few API calls.',
    speakerName: 'Ioana Chiorean',
    speakerBackground: 'Interledger, Engineering Manager',
  },
  {
    logo: '/logos/clickhouse-black-logo.svg',
    title: 'ClickHouse 101: Real-Time Analytics for AI & LLM Workloads',
    description: 'New to ClickHouse? Join this ClickHouse quickstart workshop to learn how to build fast analytical backends for AI and real-time applications.\nIn just one hour, you\'ll get an overview of ClickHouse, work hands-on with data, and explore example architectures and learning resources.\n\nWe will cover:\nClickHouse overview & use cases (10 min)\nHands-on: ingest + query data (30 min)\nAgentHouse demo (10 min)\nLearning resources & next steps (10 min)',
    speakerName: 'Maruthi Lokanathan',
    speakerBackground: 'ClickHouse, Solution Architect',
  },
  {
    logo: '/logos/google_cloud_logo_colored.png',
    logoClass: 'h-12',
    title: 'Design and Build Multi-agent Systems with ADK',
    description: 'In this hands-on workshop, you will learn how to build Multi-agent Systems using Agent Development Kit, how to design agentic applications, and how to ensure they can communicate to each other using A2A protocol.',
    speakerName: 'Thu Ya',
    speakerBackground: 'GeeksHacking, Board Member',
  },
]
