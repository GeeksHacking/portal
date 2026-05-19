import { initializeApp } from "firebase/app";

export default defineNuxtPlugin(() => {
    const firebaseConfig = {
        apiKey: "AIzaSyCqszLg2Ia6ERkpa79kqr9lgYkTo_QZjkc",
        authDomain: "hackomania-event-portal.firebaseapp.com",
        projectId: "hackomania-event-portal",
        storageBucket: "hackomania-event-portal.firebasestorage.app",
        messagingSenderId: "242247218750",
        appId: "1:242247218750:web:bc9a7e6fd1f1daf0e8836f"
    };

    const app = initializeApp(firebaseConfig);
    
    return {
        provide: {
            firebase: app
        }
    }
})