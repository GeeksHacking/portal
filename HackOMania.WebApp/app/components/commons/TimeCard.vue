<script setup lang="ts">
interface Props {
    time: string;
    label: string;
}

defineProps<Props>();
</script>

<template>
    <article
        class="w-full py-[18px] px-[24px] bg-[rgba(255,255,255,0.31)] text-left shadow-[0px_0px_2.9px_rgba(0,0,0,0.25)]"
    >
        <time
            class="block font-heading-expanded font-bold text-[20px] leading-[24px]"
        >
            {{ time }}
        </time>
        <p class="font-body font-normal text-[16px] leading-[18px]">
            {{ label }}
        </p>
    </article>
</template>
