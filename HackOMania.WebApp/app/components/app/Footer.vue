<script setup lang="ts">
import { SOCIAL_LINKS, CONTACT_EMAIL, SPONSOR_EMAIL } from '~/constants/socials';
</script>

<template>
    <div id="contact" class="w-full flex justify-center relative mt-4">
        <img
            src="/backgrounds/footer-bg.png"
            alt="Footer background"
            class="absolute -top-[200px] -bottom-0 left-0 right-0 w-full h-[calc(100%+200px)] object-fill -z-10"
        />
        <div
            class="w-full max-w-[1440px] px-8 md:px-16 lg:px-[120px] pb-16 md:pb-20 lg:pb-24 pt-16 relative z-10 flex flex-col md:flex-row justify-between"
        >
            <!-- Left Section: Contact Information -->
            <div>
                <h2
                    class="font-heading-expanded font-bold text-[22px] md:text-[32px] lg:text-[40px] text-black leading-tight"
                >
                    CONTACT US
                </h2>
                <img
                    src="/logos/geekshacking.svg"
                    alt="GeeksHacking"
                    class="mt-2 w-32 md:w-40 lg:w-auto"
                />
                <p
                    class="mt-4 md:mt-5 lg:mt-6 font-heading-expanded font-bold text-[16px] md:text-[20px] lg:text-[24px] text-black"
                >
                    FOR GENERAL ENQUIRIES
                </p>
                <p
                    class="mt-1 md:mt-2 font-body font-normal text-[14px] md:text-[16px] lg:text-[20px] text-black leading-tight"
                >
                    Drop us an email at
                    <a :href="`mailto:${CONTACT_EMAIL}`" class="underline"
                        >{{ CONTACT_EMAIL }}</a
                    >
                    or<br class="hidden md:inline" />contact us through our
                    Facebook page!
                </p>
                <p
                    class="mt-4 md:mt-5 lg:mt-6 font-heading-expanded font-bold text-[16px] md:text-[20px] lg:text-[24px] text-black"
                >
                    FOR SPONSORSHIPS
                </p>
                <p
                    class="mt-1 md:mt-2 font-body font-normal text-[14px] md:text-[16px] lg:text-[20px] text-black leading-tight"
                >
                    Please contact
                    <a :href="`mailto:${SPONSOR_EMAIL}`" class="underline"
                        >{{ SPONSOR_EMAIL }}</a
                    ><br class="hidden md:inline" />for sponsorship enquiries!
                </p>
            </div>

            <!-- Right Section: Social Media (Bottom Right) -->
            <div
                class="flex flex-col items-start md:items-end justify-end mt-8 md:mt-0"
            >
                <p
                    class="font-body font-normal text-[14px] md:text-[16px] lg:text-[20px] text-black leading-tight mb-4"
                >
                    Join GeeksHacking's <br class="hidden md:inline" />Telegram
                    Channel!
                </p>
                <div
                    class="flex gap-3 md:gap-4 text-[24px] md:text-[32px] lg:text-[40px]"
                >
                    <a
                        v-for="social in SOCIAL_LINKS"
                        :key="social.name"
                        :href="social.url"
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <Icon :name="social.icon" class="text-black" />
                    </a>
                </div>
            </div>
        </div>
    </div>
</template>
