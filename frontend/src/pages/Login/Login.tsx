import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuthStore } from "../../store/useAuthStore";

export default function Login(){
    const navigate = useNavigate();
    const {login, isLoading} = useAuthStore();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [showPassword, setShowPassword] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (!email || !password) {
            setError("Please fill in all fields");
            return;
        }

        try {
            await login({email, password});
            navigate("/dashboard");
        } catch (error:unknown) {
            setError(error.response?.data?.message || "Invalid email or password");
        }
    };
    
    
    return (<>
    <div>
        Login
    </div>
    </>)
}

