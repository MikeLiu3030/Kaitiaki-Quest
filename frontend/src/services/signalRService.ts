import * as signalR from '@microsoft/signalr';
import { enqueueSnackbar } from 'notistack';

export type UserJoinedEvent ={
    userName: string;
    message: string;
    joinedAt: string;
}

export type UserLeftEvent ={
    userName: string;
    message: string;
    leftAt: string;
}


type TeamXPUpdateEvent = {
    totalTeamXP: number;
    teamName: string;
    completedBy: string;
    missionTitle: string;
    earnedXP: number;
    updateAt: string;
}

class SignalRService { 
    private connection: signalR.HubConnection | null = null;
    private isConnecting = false;

    // // Connect to the SignalR hub
    async connect(token: string): Promise<void> {
        if (this.isConnecting) {
            console.log('SignalR is already connecting...');
            return;
        }


        if (this.connection?.state === signalR.HubConnectionState.Connected) {
            console.log('SignalR already connected');
            return;
        }

        this.isConnecting = true;

        try {
            // Cleanup existing connection before creating a new one
            if (this.connection) {
                await this.connection.stop();
            }

            const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7225';

            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(`${baseUrl}/teamHub`, {
                    accessTokenFactory: () => token,
                    transport: signalR.HttpTransportType.WebSockets,
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .build();
            
            // Register event monitoring
            this.connection.on('UserJoined', (data: UserJoinedEvent) => this.handleUserJoined(data));
            this.connection.on('UserLeft', (data: UserLeftEvent) => this.handleUserLeft(data));
            this.connection.on('TeamXPUpdated', (data: TeamXPUpdateEvent) => this.handleTeamXPUpdated(data));


            // Connection status change handlers
            this.connection.onreconnecting(() => console.log('SignalR reconnecting...'));
            this.connection.onreconnected(() => console.log('SignalR reconnected'));
            this.connection.onclose(() => console.log('SignalR connection closed'));

            await this.connection.start();
            console.log('SignalR connected');
        } catch (error) {
            console.error('SignalR connection failed:', error);
            throw error;
        } finally {
            // Always release the lock regardless of success or failure
            this.isConnecting = false;
        }
    }

    // disconnect connection
    async disconnect(): Promise<void> {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log('SignalR disconnected');
            } catch (error) {
                console.error('SignalR disconnection failed:', error);
            }
        }
    }


    // Handle the XP update event of the team
    private handleTeamXPUpdated = (data: TeamXPUpdateEvent) => {
   
        // display a notification
        if(data){
            enqueueSnackbar(
                `${data.completedBy} completed "${data.missionTitle}" and earned ${data.earnedXP} XP for team ${data.teamName}!`,
                {
                    variant: 'success',
                }
            );
        };

        // Trigger custom events to enable the page to listen and update data
        window.dispatchEvent(new CustomEvent('teamXPUpdated', { detail: data }));
    };


    // Handle the user joined event
    private handleUserJoined = (data: UserJoinedEvent) => {
        enqueueSnackbar(
            `🎉 ${data.userName} ${data.message}`, 
            { variant: 'success' } 
        );
        window.dispatchEvent(new CustomEvent('teamMemberJoined', { detail: data }));
    };


    //Handle the user left event
    private handleUserLeft = (data: UserLeftEvent) => {
        enqueueSnackbar(
            `👋 ${data.userName} ${data.message}`, 
            { variant: 'default' } 
        );
        window.dispatchEvent(new CustomEvent('teamMemberLeft', { detail: data }));
    };

    

    // Public getter for connection state
    get isConnected(): boolean {
        return this.connection?.state === signalR.HubConnectionState.Connected;
    }

    get connectionId(): string | null {
        return this.connection?.connectionId || null;
    }

}

export const signalRService = new SignalRService();
