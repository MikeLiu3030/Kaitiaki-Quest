import { isAxiosError } from "axios";
export default function getApiErrorMsg(err: unknown):string {
    if (isAxiosError(err)){
        return err.response?.data?.message || err.message || 'Network Error';
    }
    if (err instanceof Error){
        return err.message;
    }
    return 'Unknown Error occurred';        
};

