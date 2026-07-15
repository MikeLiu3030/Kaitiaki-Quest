import * as signalR from '@microsoft/signalr';
import { enqueueSnackbar } from 'notistack';

type TeamXPUpdateEvent = {
    totalTeamXp: number;
    teamName: string;
    completedBy: string;
    missionTitle: string;
    earnedXP: number;
    updateAt: string;
}

class SignalRService { 
    private connection: signalR.HubConnection | null = null;
    private isConnected = false;

    // create connection
    async connect(token: string): Promise<void> { 
        if (this.isConnected){
            console.log('SignalR already connected')
            return;
        }

        const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7225';

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/teamHub`, {
                accessTokenFactory: () => token,
                transport: signalR.HttpTransportType.WebSockets,
            })
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .build();

        // register event monitoring
        this.connection.on('TeamXPUpdated', this.handleTeamXPUpdated);

        // connection status change
        this.connection.onreconnecting(() => { 
            console.log('SignalR reconnecting...');
            this.isConnected = false;
        });

        this.connection.onreconnected(() => { 
            console.log('SignalR reconnected');
            this.isConnected = true;
        });

        this.connection.onclose(() => { 
            console.log('SignalR connection closed');
            this.isConnected = false;
        });

        try {
            await this.connection.start();
            this.isConnected = true;
            console.log('SignalR connected');
        }catch (error) { 
            console.error('SignalR connection failed:', error);
        }            
    }

    // disconnect connection
    async disconnect(): Promise<void> {
        if (this.connection) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                console.log('SignalR disconnected');
            } catch (error) {
                console.error('SignalR disconnection failed:', error);
            }
        }
    }

    // join team room
    async joinTeamRoom(teamId: number): Promise<void> {
        // check connection status
        if (!this.isConnected || !this.connection) {
            console.warn("SignalR not connected, cannot join team room.");
            return;
        }
        try {
            await this.connection.invoke('JoinTeamRoom', teamId.toString());
            console.log(`Joined team room ${teamId}`);
        } catch (error) {
            console.error('Failed to join team room:', error);

        }
    }

    // leave team room
    async leaveTeamRoom(teamId: number): Promise<void> {
        // check connection status
        if (!this.isConnected || !this.connection) {
            console.warn("SignalR not connected, cannot leave team room.");
            return;
        }
        try {
            await this.connection.invoke('LeaveTeamRoom', teamId.toString());
            console.log(`Left team room ${teamId}`);
        } catch (error) {
            console.error('Failed to leave team room:', error);
        }
    }

    // Handle the XP update event of the team
    private handleTeamXPUpdated = (data: TeamXPUpdateEvent) => {
        console.log('Team XP updated:', data); 
        // display a notification
        enqueueSnackbar(
            `${data.completedBy} completed "${data.missionTitle}" and earned ${data.earnedXP} XP for team ${data.teamName}!`,
            {
                variant: 'success',
                autoHideDuration: 5000,
                anchorOrigin: { vertical: 'top', horizontal: 'right' },
            }
        );

        // Trigger custom events to enable the page to listen and update data
        window.dispatchEvent(new CustomEvent('teamXPUpdated', { detail: data }));
    };

    //Check connection status
    get isConnectedStatus(): boolean { 
        return this.isConnected;
    }
}

export const signalRService = new SignalRService();
