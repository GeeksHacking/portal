<template>
  <div id="contacts" class="w-full flex justify-center relative mt-4 md:mt-16 lg:mt-32">
    <NuxtImg
      src="/backgrounds/footer-bg.png"
      alt="Footer background"
      class="absolute -top-50 left-0 right-0 w-full h-[calc(100%+200px)] object-fill -z-10"
    />
    <div
      class="w-full max-w-360 px-8 lg:px-32 pb-16 lg:pb-24 pt-16 relative z-10 flex flex-col lg:flex-row justify-between"
    >
      <!-- Left Section: Contact Information -->
      <div class="max-w-md">
        <h2
          class="font-['Zalando_Sans_Expanded'] font-bold text-2xl lg:text-3xl text-black leading-tight"
        >
          CONTACT US
        </h2>
        <NuxtImg
          src="/logos/geekshacking.svg"
          alt="GeeksHacking"
          class="mt-2 w-32 lg:w-56"
        />
        <p
          class="mt-4 lg:mt-6 font-['Zalando_Sans_Expanded'] font-bold text-base lg:text-lg text-black"
        >
          FOR GENERAL ENQUIRIES
        </p>
        <p
          class="mt-1 font-['Raleway'] font-normal text-sm lg:text-base text-black leading-tight"
        >
          Drop us an email at
          <a
            :href="`mailto:${CONTACT_EMAIL}`"
            class="underline"
          >{{ CONTACT_EMAIL }}</a>
          or contact us through our Facebook page!
        </p>
        <p
          class="mt-4 lg:mt-6 font-['Zalando_Sans_Expanded'] font-bold text-base lg:text-lg text-black"
        >
          FOR SPONSORSHIPS
        </p>
        <p
          class="mt-1 font-['Raleway'] font-normal text-sm lg:text-base text-black leading-tight"
        >
          Please contact
          <a
            :href="`mailto:${SPONSOR_EMAIL}`"
            class="underline"
          >{{ SPONSOR_EMAIL }}</a>
          for sponsorship enquiries!
        </p>
      </div>

      <!-- Right Section: Social Media (Bottom Right) -->
      <div
        class="flex flex-col items-start lg:items-end justify-end mt-8 lg:mt-0 max-w-3xs"
      >
        <p
          class="font-['Raleway'] font-normal text-sm lg:text-base text-black leading-tight mb-4 lg:text-right"
        >
          Join GeeksHacking's Telegram Channel!
        </p>
        <div
          class="flex gap-3 lg:gap-4 text-2xl lg:text-3xl"
        >
          <a
            v-for="social in SOCIAL_LINKS"
            :key="social.name"
            :href="social.url"
            target="_blank"
            rel="noopener noreferrer"
          >
            <Icon
              :name="social.icon"
              class="text-black"
            />
          </a>
        </div>
      </div>
    </div>
  </div>
</template>
