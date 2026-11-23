<script setup lang="ts">
/**
 * The camera view for scanning participant QR codes (html5-qrcode, loaded on demand).
 *
 * Starts the camera when it mounts and releases it when it unmounts, so the parent shows it with
 * v-if only while it wants to scan. After the first code it stops and emits `decoded`; mount it
 * again to scan the next one. Reads most of the picture (70%), not a small square, so codes don't
 * have to be lined up exactly. Explains a blocked camera and offers "Try again".
 */
const emit = defineEmits<{ decoded: [text: string] }>()

const elementId = `qr-reader-${useId()}`
const isCameraOn = ref(false)
const cameraError = ref('')
const useFrontCamera = ref(false)
const cameraCount = ref(0)

let qr: import('html5-qrcode').Html5Qrcode | null = null
let decoded = false
let unmounted = false

async function waitForElement() {
  for (let i = 0; i < 60 && !document.getElementById(elementId); i++)
    await new Promise(resolve => requestAnimationFrame(resolve))
  return !!document.getElementById(elementId)
}

async function start() {
  cameraError.value = ''
  decoded = false
  if (!(await waitForElement()) || unmounted)
    return
  try {
    const { Html5Qrcode } = await import('html5-qrcode')
    if (unmounted)
      return
    qr = new Html5Qrcode(elementId)
    if (!cameraCount.value)
      cameraCount.value = (await Html5Qrcode.getCameras().catch(() => [])).length
    await qr.start(
      { facingMode: useFrontCamera.value ? 'user' : 'environment' },
      {
        fps: 30,
        qrbox: (width: number, height: number) => {
          const side = Math.max(160, Math.floor(Math.min(width, height) * 0.7))
          return { width: side, height: side }
        },
        aspectRatio: 1,
      },
      (text) => {
        if (decoded)
          return
        decoded = true
        void stop().then(() => emit('decoded', text))
      },
      () => {},
    )
    isCameraOn.value = true
    if (unmounted)
      await stop()
  }
  catch (error) {
    console.error('Failed to start camera', error)
    cameraError.value = 'Allow camera access for this site, then try again. You can also find people in the list.'
    isCameraOn.value = false
  }
}

async function stop() {
  isCameraOn.value = false
  const current = qr
  qr = null
  if (!current)
    return
  try {
    await current.stop()
    current.clear()
  }
  catch (error) {
    console.error('Error stopping scanner', error)
  }
}

async function switchCamera() {
  useFrontCamera.value = !useFrontCamera.value
  await stop()
  await start()
}

onMounted(() => {
  void start()
})
onBeforeUnmount(() => {
  unmounted = true
  void stop()
})
</script>

<template>
  <div class="qs">
    <div
      :id="elementId"
      class="qs__view"
    />
    <GAlert
      v-if="cameraError"
      tone="err"
      title="Camera unavailable"
      :description="cameraError"
    />
    <div
      v-if="cameraError || (isCameraOn && cameraCount > 1)"
      class="flex flex-wrap justify-center gap-2"
    >
      <GButton
        v-if="cameraError"
        variant="secondary"
        icon="material-symbols:refresh-rounded"
        label="Try again"
        @click="start"
      />
      <GButton
        v-if="isCameraOn && cameraCount > 1"
        variant="ghost"
        icon="material-symbols:cameraswitch-rounded"
        label="Switch camera"
        @click="switchCamera"
      />
    </div>
  </div>
</template>

<style scoped>
.qs {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.qs__view {
  width: 100%;
  min-height: 240px;
  overflow: hidden;
  border-radius: var(--radius-gh-sm);
  background: #000;
}
.qs__view:empty {
  aspect-ratio: 1;
  min-height: 0;
}
</style>
